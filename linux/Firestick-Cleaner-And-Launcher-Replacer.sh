#!/usr/bin/env bash
# Firestick Cleanup Tool - Projectivy Launcher + Debloat (Mac/Linux)

set -uo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

ADB="$SCRIPT_DIR/adb/adb"
LOGFILE="$SCRIPT_DIR/firestick_cleanup.log"
PROJECTIVY_PKG="com.spocky.projengmenu"
PROJECTIVY_ACTIVITY="$PROJECTIVY_PKG/.ui.home.MainActivity"
PROJECTIVY_ACCESSIBILITY="$PROJECTIVY_PKG/.services.ProjectivyAccessibilityService"
PROJECTIVY_NOTIFICATION="$PROJECTIVY_PKG/.services.notification.NotificationListener"
FALLBACK_APK_URL="https://github.com/spocky/miproja1/releases/download/4.73/ProjectivyLauncher-4.73-c98-xda-release.apk"

# Mac: strip the quarantine attribute Gatekeeper adds to downloaded binaries
if [ "$(uname)" = "Darwin" ]; then
    xattr -dr com.apple.quarantine "$SCRIPT_DIR/adb" >/dev/null 2>&1 || true
fi
chmod +x "$ADB" 2>/dev/null || true

# File size helper (BSD/macOS uses -f, GNU/Linux uses -c)
filesize() {
    stat -f%z "$1" 2>/dev/null || stat -c%s "$1" 2>/dev/null || echo 0
}

# Yes-prompt that works on bash 3.2 (no ${var,,})
yesno() {
    local prompt="$1"
    local reply
    read -p "$prompt" reply
    case "$reply" in
        [Yy]*) return 0 ;;
        *) return 1 ;;
    esac
}

banner() {
    echo
    echo "  ========================================"
    echo "    $1"
    echo "  ========================================"
}

{
    echo "============================================"
    echo "  Firestick Cleanup Log"
    echo "  Started: $(date)"
    echo "============================================"
    echo
} > "$LOGFILE"

REVERT=0
if [ "${1:-}" = "--revert" ]; then
    REVERT=1
fi

banner "Firestick Cleanup Tool"
echo "    Projectivy Launcher + Debloat"
echo

if [ ! -x "$ADB" ]; then
    echo "  [!!] ADB not found or not executable at: $ADB"
    echo "  [!!] Make sure the platform-tools folder is next to this script."
    read -p "  Press Enter to exit..."
    exit 1
fi
echo "  [OK] ADB found: $ADB"

read -p "  Enter your Fire TV IP address (e.g. 10.0.0.20): " FIRE_IP
if [ -z "$FIRE_IP" ]; then
    echo "  [!!] No IP provided."
    read -p "  Press Enter to exit..."
    exit 1
fi

TARGET_OPT="-s $FIRE_IP:5555"
echo "[CONFIG] IP=$FIRE_IP TARGET=$TARGET_OPT" >> "$LOGFILE"

"$ADB" disconnect >/dev/null 2>&1 || true

# ============================================================
# REVERT MODE
# ============================================================
if [ $REVERT -eq 1 ]; then
    banner "REVERTING ALL CHANGES"

    "$ADB" connect "$FIRE_IP:5555" >/dev/null 2>&1
    echo
    echo "  A connection request has been sent to your Fire TV."
    echo "  On your TV screen, select 'Always allow from this computer' and press OK."
    echo
    read -p "  Press Enter once authorized..."
    "$ADB" connect "$FIRE_IP:5555" >>"$LOGFILE" 2>&1
    sleep 2

    if ! "$ADB" $TARGET_OPT shell echo ok 2>&1 | grep -q "ok"; then
        echo "  [!!] Could not connect to Fire TV."
        read -p "  Press Enter to exit..."
        exit 1
    fi
    echo "  [OK] Connected"

    echo "  [..] Re-enabling disabled packages..."
    disabled_list=$("$ADB" $TARGET_OPT shell pm list packages -d 2>&1)
    echo "$disabled_list" | sed 's/^package://' | while read -r pkg; do
        pkg=$(echo "$pkg" | tr -d '\r')
        [ -z "$pkg" ] && continue
        "$ADB" $TARGET_OPT shell pm enable "$pkg" >/dev/null 2>&1
        echo "  [OK] Enabled: $pkg"
    done

    "$ADB" $TARGET_OPT shell settings put secure enabled_accessibility_services '""' >/dev/null 2>&1
    "$ADB" $TARGET_OPT shell settings put secure accessibility_enabled 0 >/dev/null 2>&1
    echo "  [OK] Accessibility service removed"

    "$ADB" $TARGET_OPT shell settings put secure enabled_notification_listeners '""' >/dev/null 2>&1
    echo "  [OK] Notification listener removed"

    "$ADB" $TARGET_OPT shell appops set "$PROJECTIVY_PKG" SYSTEM_ALERT_WINDOW deny >/dev/null 2>&1
    echo "  [OK] Overlay permission revoked"

    "$ADB" $TARGET_OPT shell cmd role remove-role-holder android.app.role.HOME "$PROJECTIVY_PKG" >/dev/null 2>&1
    echo "  [OK] HOME role removed"

    if "$ADB" $TARGET_OPT shell pm uninstall "$PROJECTIVY_PKG" 2>&1 | grep -q "Success"; then
        echo "  [OK] Projectivy uninstalled"
    else
        echo "  [--] Could not uninstall Projectivy (may not be installed)"
    fi

    banner "REVERT COMPLETE"
    read -p "  Press Enter to exit..."
    exit 0
fi

# ============================================================
# STEP 1: Connect
# ============================================================
banner "STEP 1: Connecting to Fire TV at $FIRE_IP"
"$ADB" connect "$FIRE_IP:5555" >/dev/null 2>&1

echo
echo "  A connection request has been sent to your Fire TV."
echo "  On your TV screen, select 'Always allow from this computer' and press OK."
echo
read -p "  Press Enter once authorized..."

"$ADB" connect "$FIRE_IP:5555" >>"$LOGFILE" 2>&1
sleep 2

if "$ADB" $TARGET_OPT shell echo ok 2>&1 | grep -q "ok"; then
    echo "  [OK] Connected to $FIRE_IP:5555"
    echo "[STEP 1] Connected OK" >> "$LOGFILE"
else
    echo "  [!!] Could not connect. Make sure:"
    echo "      1. Fire TV and this computer are on the same network"
    echo "      2. ADB Debugging is enabled on the Fire TV"
    echo "      3. You authorized the connection on the TV screen"
    read -p "  Press Enter to exit..."
    exit 1
fi

# ============================================================
# BASELINE: Capture RAM before cleanup
# ============================================================
banner "BASELINE: Capturing RAM Usage"

mem_total_kb=$("$ADB" $TARGET_OPT shell "cat /proc/meminfo | grep MemTotal" 2>/dev/null | awk '{print $2}' | tr -d '\r')
mem_avail_kb=$("$ADB" $TARGET_OPT shell "cat /proc/meminfo | grep MemAvailable" 2>/dev/null | awk '{print $2}' | tr -d '\r')
RAM_BEFORE_TOTAL=$(( ${mem_total_kb:-0} / 1024 ))
RAM_BEFORE_AVAIL=$(( ${mem_avail_kb:-0} / 1024 ))
RAM_BEFORE_USED=$(( RAM_BEFORE_TOTAL - RAM_BEFORE_AVAIL ))
PROCS_BEFORE=$("$ADB" $TARGET_OPT shell "ps -A | grep com.amazon | wc -l" 2>/dev/null | tr -d '[:space:]')
PROCS_BEFORE=${PROCS_BEFORE:-0}

echo "  [OK] Total RAM:        $RAM_BEFORE_TOTAL MB"
echo "  [OK] Available RAM:    $RAM_BEFORE_AVAIL MB"
echo "  [OK] Used RAM:         $RAM_BEFORE_USED MB"
echo "  [OK] Amazon processes: $PROCS_BEFORE running"

# ============================================================
# STEP 2: Download Projectivy
# ============================================================
banner "STEP 2: Downloading Projectivy Launcher"

APK_FILE=""
for f in "$SCRIPT_DIR"/ProjectivyLauncher-*.apk; do
    if [ -f "$f" ]; then
        APK_FILE="$f"
        break
    fi
done

if [ -n "$APK_FILE" ]; then
    echo "  [OK] APK already exists: $APK_FILE"
else
    echo "  [..] Fetching latest release info from GitHub..."
    APK_URL=$(curl -s "https://api.github.com/repos/spocky/miproja1/releases/latest" 2>/dev/null \
        | grep "browser_download_url" \
        | grep "\.apk" \
        | head -1 \
        | sed -E 's/.*"(https?:\/\/[^"]+\.apk)".*/\1/')

    if [ -z "$APK_URL" ]; then
        echo "  [..] Could not reach the GitHub API (offline or rate-limited)."
        echo "       Falling back to pinned Projectivy 4.73; it may not be the newest."
        APK_URL="$FALLBACK_APK_URL"
    fi

    APK_FILE="$SCRIPT_DIR/ProjectivyLauncher-latest.apk"
    echo "  [..] Downloading: $APK_URL"
    if ! curl -L -o "$APK_FILE" "$APK_URL"; then
        echo "  [!!] Download failed."
        read -p "  Press Enter to exit..."
        exit 1
    fi

    APK_SIZE=$(filesize "$APK_FILE")
    if [ -z "$APK_SIZE" ] || [ "$APK_SIZE" -lt 100000 ]; then
        echo "  [!!] Download failed - file too small."
        rm -f "$APK_FILE"
        read -p "  Press Enter to exit..."
        exit 1
    fi
    echo "  [OK] Downloaded ($(( APK_SIZE / 1048576 )) MB)"
fi

# ============================================================
# STEP 3: Install Projectivy
# ============================================================
banner "STEP 3: Installing Projectivy Launcher"

echo "  [..] Pushing APK to device..."
push_out=$("$ADB" $TARGET_OPT push "$APK_FILE" /data/local/tmp/projectivy.apk 2>&1)
echo "$push_out" >> "$LOGFILE"
if ! echo "$push_out" | grep -q "pushed"; then
    echo "  [!!] Failed to push APK to device."
    echo "$push_out"
    read -p "  Press Enter to exit..."
    exit 1
fi
echo "  [OK] APK pushed to device"

echo "  [..] Installing..."
inst_out=$("$ADB" $TARGET_OPT shell pm install -r /data/local/tmp/projectivy.apk 2>&1)
echo "$inst_out" >> "$LOGFILE"
if ! echo "$inst_out" | grep -q "Success"; then
    echo "  [!!] Install failed."
    echo "$inst_out"
    read -p "  Press Enter to exit..."
    exit 1
fi

if "$ADB" $TARGET_OPT shell pm path "$PROJECTIVY_PKG" 2>&1 | grep -q "package:"; then
    echo "  [OK] Projectivy Launcher installed and verified"
else
    echo "  [!!] Install reported success but package not found on device."
    read -p "  Press Enter to exit..."
    exit 1
fi

"$ADB" $TARGET_OPT shell rm /data/local/tmp/projectivy.apk >/dev/null 2>&1 || true

# ============================================================
# STEP 4: Configure as default launcher
# ============================================================
banner "STEP 4: Configuring Projectivy"

"$ADB" $TARGET_OPT shell settings put secure enabled_accessibility_services "$PROJECTIVY_ACCESSIBILITY" >>"$LOGFILE" 2>&1
"$ADB" $TARGET_OPT shell settings put secure accessibility_enabled 1 >>"$LOGFILE" 2>&1

if "$ADB" $TARGET_OPT shell settings get secure enabled_accessibility_services 2>&1 | grep -q "$PROJECTIVY_PKG"; then
    echo "  [OK] Accessibility service enabled"
else
    echo "  [!!] Could not enable accessibility service"
    read -p "  Press Enter to exit..."
    exit 1
fi

"$ADB" $TARGET_OPT shell settings put secure enabled_notification_listeners "$PROJECTIVY_NOTIFICATION" >>"$LOGFILE" 2>&1
echo "  [OK] Notification listener enabled"

"$ADB" $TARGET_OPT shell appops set "$PROJECTIVY_PKG" SYSTEM_ALERT_WINDOW allow >>"$LOGFILE" 2>&1
"$ADB" $TARGET_OPT shell dumpsys deviceidle whitelist +"$PROJECTIVY_PKG" >>"$LOGFILE" 2>&1
echo "  [OK] Overlay permission granted"

"$ADB" $TARGET_OPT shell cmd role add-role-holder android.app.role.HOME "$PROJECTIVY_PKG" >>"$LOGFILE" 2>&1
echo "  [OK] HOME role assigned"

launch_out=$("$ADB" $TARGET_OPT shell am start -n "$PROJECTIVY_ACTIVITY" 2>&1)
echo "$launch_out" >> "$LOGFILE"
if echo "$launch_out" | grep -q "Error"; then
    echo "  [..] Launch failed, trying alternative method..."
    monkey_out=$("$ADB" $TARGET_OPT shell monkey -p "$PROJECTIVY_PKG" -c android.intent.category.LEANBACK_LAUNCHER 1 2>&1)
    if echo "$monkey_out" | grep -q "Events injected"; then
        echo "  [OK] Projectivy launched via alternative method"
    else
        echo "  [!!] Could not launch Projectivy automatically."
        echo "  [!!] Please open it manually from your Fire TV app list."
    fi
else
    echo "  [OK] Projectivy launched"
fi

cat <<'EOF'

  +---------------------------------------------------------+
  |  ACTION REQUIRED on your Fire TV:                       |
  |                                                         |
  |  1. Projectivy should now be on your screen             |
  |  2. Go to Projectivy Settings (long-press center btn)   |
  |  3. Select 'General'                                    |
  |  4. Enable 'Override current launcher'                  |
  |                                                         |
  |  This makes the Home button open Projectivy instead     |
  |  of the Amazon launcher.                                |
  +---------------------------------------------------------+

EOF
read -p "  Press Enter when done..."

# ============================================================
# STEP 5: Optional apps
# ============================================================
banner "STEP 5: Choose What to Disable"
echo
echo "  The following Amazon apps can be disabled. Some you may"
echo "  actually use - answer Y/N for each one."
echo "  (Tracking, telemetry, and bloat are always disabled.)"
echo

OPTIONAL_BLOAT=""

echo "  Note: answer N to the next question if you use Prime Video,"
echo "        Freevee, or MiniTV - this disables the Prime Video app"
echo "        and the playback services those apps need."
if yesno "  Disable Amazon Video services?         (Y/N): "; then
    OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.firebat com.amazon.avls.experience com.amazon.prism.android.service com.amazon.dp.logger com.amazon.livedeviceservice com.amazon.rtcsessioncontroller com.amazon.client.metrics.api com.amazon.tv.ottssocompanionapp"
fi

yesno "  Disable Amazon Appstore?              (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.venezia"
yesno "  Disable Amazon Photos?                 (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.bueller.photos"
yesno "  Disable Amazon Music?                  (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.bueller.music"
yesno "  Disable Freevee / IMDb TV?              (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.imdb.tv.android.app"
yesno "  Disable Amazon MiniTV?                 (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.minitv.android.app"
yesno "  Disable Amazon Game Hub?               (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.gamehub"
yesno "  Disable Amazon Live TV?                (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.tv.livetv com.amazon.franktvinput com.amazon.tifobserver"
yesno "  Disable Alexa alerts/notifications?    (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.tv.alexaalerts com.amazon.tv.alexanotifications com.amazon.audiohome"
yesno "  Disable Alexa extras? (hints, multi-room audio, Alexa music; voice search keeps working) (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.aca com.amazon.aiondec com.amazon.alexa.datastore.app com.amazon.alexadirectivebrokerservice com.amazon.alexa.externalmediaplayer.fireos com.amazon.alexamediaplayer.runtime.ftv com.amazon.whad com.amazon.whasettings com.amazon.wha.mediabrowserservice com.amazon.spotify.mediabrowserservice"
yesno "  Disable Silk Browser (Amazon Cloud9)?  (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.cloud9"
yesno "  Disable Smart Home features?            (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.smarthomemapviewapp com.amazon.tv.matter com.amazon.tv.mattercohost"
yesno "  Disable WhisperPlay (casting)?         (Y/N): " && OPTIONAL_BLOAT="$OPTIONAL_BLOAT com.amazon.whisperplay.service.install com.amazon.awvflingreceiver com.amazon.cast.sink com.amazon.dialservice com.amazon.tcomm.jackson"

echo
echo "  [OK] Preferences saved. All tracking/telemetry/bloat will"
echo "       always be disabled regardless of choices above."

# ============================================================
# STEP 6: Disable bloatware
# ============================================================
BLOAT="com.amazon.tv.acr com.amazon.hybridadidservice com.amazon.perfc com.amazon.perfcollection com.amazon.device.telemetry.emitter com.amazon.wirelessmetrics.service com.amazon.shoptv.client com.amazon.shoptv.firetv.client com.amazon.sneakpeek com.amazon.ftv.screensaver com.amazon.storm.lightning.tutorial com.amazon.tmm.tutorial com.amazon.tv.releasenotes com.amazon.device.rdmapplication com.amazon.fireos.cirruscloud com.amazon.ods.kindleconnect com.amazon.tahoe com.amazon.aria com.amazon.hedwig com.amazon.tv.support com.amazon.ceviche com.amazon.d3 com.amazon.tv.turnstile com.amazon.tv.ftvambient com.amazon.wifilocker com.amazon.spiderpork com.amazon.tv.notificationcenter com.amazon.ssm com.amazon.ssmsys com.amazon.tv.easyupgrade com.amazon.dpcclient com.amazon.sharingservice.android.client.proxy com.amazon.privacypassservice com.amazon.tv.legal.notices com.amazon.kso.blackbird com.amazon.tv.developer.dataservice com.amazon.media.recommendations com.amazon.uxnotification com.amazon.systemnotices com.amazon.tv.website_launcher com.amazon.tv.ffsprovisioneeclient com.amazon.whisperjoin.middleware.np com.amazon.whisperjoin.middleware.v2.np com.amazon.tv.legalresources com.amazon.device.sale.service com.amazon.neo.minerva com.amazon.neo.minerva.logcat com.amazon.tv.fw.metrics com.amazon.dummy.alarmclock com.amazon.dummy.calendar com.amazon.dummy.contacts com.amazon.dummy.gallery com.amazon.dummy.music com.amazon.dummy.soundpicker com.android.cts.ctsshim com.android.cts.priv.ctsshim com.android.protips com.android.dreams.basic com.android.printspooler com.android.bips com.android.printservice.recommendation com.android.bookmarkprovider com.android.calllogbackup com.android.smspush com.android.mms.service com.android.hotspot2.osulogin com.android.traceur com.android.onetimeinitializer com.android.carrierconfig com.android.safetyregulatoryinfo com.android.htmlviewer"
BLOAT="$BLOAT $OPTIONAL_BLOAT"

DONE_PKGS=" "
TOTAL_DISABLED=0
PASS=0

reboot_and_reconnect() {
    "$ADB" $TARGET_OPT shell reboot >/dev/null 2>&1
    echo "  [..] Waiting 35 seconds..."
    sleep 35
    local i
    for i in $(seq 1 20); do
        "$ADB" connect "$FIRE_IP:5555" >/dev/null 2>&1
        if "$ADB" $TARGET_OPT shell echo ok 2>&1 | grep -q "ok"; then
            sleep 5
            echo "  [OK] Device is back online"
            return 0
        fi
        echo "  [..] Waiting for reboot... ($i/20)"
        sleep 3
    done
    return 1
}

while true; do
    PASS=$(( PASS + 1 ))
    PASS_DISABLED=0
    PASS_PROTECTED=0

    banner "STEP 6: Disabling Bloatware (Pass $PASS)"
    echo "[PASS $PASS] Starting disable pass..." >> "$LOGFILE"

    for pkg in $BLOAT; do
        case "$DONE_PKGS" in
            *" $pkg "*) continue ;;
        esac

        out=$("$ADB" $TARGET_OPT shell pm disable-user --user 0 "$pkg" 2>&1)
        echo "[PASS $PASS] $pkg => $out" >> "$LOGFILE"

        if echo "$out" | grep -q "disabled-user"; then
            echo "  [OK] Disabled: $pkg"
            DONE_PKGS="$DONE_PKGS$pkg "
            PASS_DISABLED=$(( PASS_DISABLED + 1 ))
            TOTAL_DISABLED=$(( TOTAL_DISABLED + 1 ))
        elif echo "$out" | grep -q "SecurityException"; then
            echo "  [--] Protected: $pkg"
            PASS_PROTECTED=$(( PASS_PROTECTED + 1 ))
        else
            echo "  [--] Skipped: $pkg (not found on device)"
            DONE_PKGS="$DONE_PKGS$pkg "
        fi
    done

    echo
    echo "  Pass $PASS: Disabled $PASS_DISABLED, Protected $PASS_PROTECTED"

    if [ $PASS_DISABLED -gt 0 ] && [ $PASS_PROTECTED -gt 0 ] && [ $PASS -lt 3 ]; then
        echo
        echo "  [..] Some packages protected - rebooting to retry..."
        if ! reboot_and_reconnect; then
            echo "  [!!] Could not reconnect. Continuing with what we have..."
            break
        fi
        continue
    fi
    break
done

if [ $PASS -le 1 ]; then
    echo
    echo "  [..] Rebooting to apply changes..."
    if ! reboot_and_reconnect; then
        echo "  [!!] Could not reconnect after reboot."
        read -p "  Press Enter to exit..."
        exit 1
    fi
fi

echo
echo "  [..] Re-applying disabled packages after reboot..."
REAPPLIED=0
disabled_now=$("$ADB" $TARGET_OPT shell pm list packages -d 2>&1)
for pkg in $DONE_PKGS; do
    [ -z "$pkg" ] && continue
    if ! echo "$disabled_now" | grep -q "package:$pkg"; then
        out=$("$ADB" $TARGET_OPT shell pm disable-user --user 0 "$pkg" 2>&1)
        if echo "$out" | grep -q "disabled-user"; then
            REAPPLIED=$(( REAPPLIED + 1 ))
        fi
    fi
done
if [ $REAPPLIED -gt 0 ]; then
    echo "  [OK] Re-disabled $REAPPLIED packages that Amazon re-enabled"
else
    echo "  [OK] All packages stayed disabled after reboot"
fi

# ============================================================
# STEP 7: Verify
# ============================================================
banner "STEP 7: Verifying Persistence"

if "$ADB" $TARGET_OPT shell pm list packages 2>&1 | grep -q "$PROJECTIVY_PKG"; then
    echo "  [OK] Projectivy is installed"
else
    echo "  [!!] Projectivy is NOT installed"
fi

if "$ADB" $TARGET_OPT shell settings get secure enabled_accessibility_services 2>&1 | grep -q "$PROJECTIVY_PKG"; then
    echo "  [OK] Accessibility service is active"
else
    echo "  [!!] Accessibility service is NOT active"
fi

DISABLED_COUNT=$("$ADB" $TARGET_OPT shell pm list packages -d 2>&1 | grep -c "package:")
echo "  [OK] $DISABLED_COUNT packages remain disabled"

# ============================================================
# RAM comparison
# ============================================================
banner "RAM Usage: Before vs After Cleanup"

mem_total_kb=$("$ADB" $TARGET_OPT shell "cat /proc/meminfo | grep MemTotal" 2>/dev/null | awk '{print $2}' | tr -d '\r')
mem_avail_kb=$("$ADB" $TARGET_OPT shell "cat /proc/meminfo | grep MemAvailable" 2>/dev/null | awk '{print $2}' | tr -d '\r')
RAM_AFTER_TOTAL=$(( ${mem_total_kb:-0} / 1024 ))
RAM_AFTER_AVAIL=$(( ${mem_avail_kb:-0} / 1024 ))
RAM_AFTER_USED=$(( RAM_AFTER_TOTAL - RAM_AFTER_AVAIL ))
PROCS_AFTER=$("$ADB" $TARGET_OPT shell "ps -A | grep com.amazon | wc -l" 2>/dev/null | tr -d '[:space:]')
PROCS_AFTER=${PROCS_AFTER:-0}

RAM_FREED=$(( RAM_AFTER_AVAIL - RAM_BEFORE_AVAIL ))
PROCS_KILLED=$(( PROCS_BEFORE - PROCS_AFTER ))

echo
printf "                        %-13s %-13s %s\n" "BEFORE" "AFTER" "CHANGE"
echo "  ---------------------------------------------------------"
printf "  Available RAM:    %5d MB      %5d MB      +%d MB freed\n" "$RAM_BEFORE_AVAIL" "$RAM_AFTER_AVAIL" "$RAM_FREED"
printf "  Used RAM:         %5d MB      %5d MB\n" "$RAM_BEFORE_USED" "$RAM_AFTER_USED"
printf "  Amazon processes: %5d            %5d            -%d removed\n" "$PROCS_BEFORE" "$PROCS_AFTER" "$PROCS_KILLED"
echo "  ---------------------------------------------------------"
echo

banner "ALL DONE - Firestick Cleanup Complete!"
echo
echo "  Projectivy Launcher is installed and configured."
echo "  $TOTAL_DISABLED bloatware packages disabled."
echo "  $RAM_FREED MB of RAM freed."
echo
echo "  To revert all changes, run:"
echo "    ./$(basename "$0") --revert"
echo
echo "  Full log saved to: $LOGFILE"
echo

rm -f "$SCRIPT_DIR/ProjectivyLauncher-latest.apk"
read -p "  Press Enter to exit..."
