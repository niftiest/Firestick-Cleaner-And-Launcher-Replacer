# Firestick Cleanup

![Firestick Cleanup screenshot](screenshot.jpg)

Automated tool to replace the ad-filled Amazon Fire TV launcher with [Projectivy Launcher](https://github.com/spocky/miproja1) and disable Amazon bloatware — no root required.

## What It Does

1. Connects to your Fire TV over ADB (WiFi)
2. Downloads and installs the latest Projectivy Launcher
3. Configures Projectivy as the default launcher
4. Asks which optional Amazon apps you want to keep (Music, Photos, Appstore, etc.)
5. Disables all tracking/telemetry/bloat automatically + your optional choices
6. Reboots, verifies persistence, and re-applies anything Amazon re-enables
7. Shows before/after RAM comparison

## Platforms

Three implementations are included — pick the one for your OS:

| Folder | OS | Entry point | UI |
|--------|-----|-------------|------|
| `windows/` | Windows | `Firestick-Cleaner-And-Launcher-Replacer.bat` *or* `WinformVer/` (`FirestickCleanup.exe`) | Console batch or WinForms GUI |
| `mac/` | macOS | `Firestick-Cleaner-And-Launcher-Replacer.command` | Terminal |
| `linux/` | Linux | `Firestick-Cleaner-And-Launcher-Replacer.sh` | Terminal |

All three bundle their own ADB binary and produce the same result. The Mac and Linux scripts share identical logic — only the filename and ADB binary differ.

## Prerequisites

### ADB (Android Debug Bridge)

ADB is **bundled** with each platform variant — no separate installation needed:

- Windows: `windows/adb/adb.exe`
- macOS: `mac/adb/adb`
- Linux: `linux/adb/adb`

### Enable ADB on Fire TV

1. Go to **Settings > My Fire TV > About**
2. Click **Fire TV Stick** (the device name) 7 times to enable Developer Options
3. Go back to **Settings > My Fire TV > Developer Options**
4. Enable **ADB Debugging**
5. Enable **Apps from Unknown Sources**
6. Note your Fire TV's IP address from **Settings > My Fire TV > About > Network**

## Usage

### Windows

Double-click `windows/Firestick-Cleaner-And-Launcher-Replacer.bat` for the console version, or build/run `windows/WinformVer/FirestickCleanup.exe` for the GUI.

To revert: `Firestick-Cleaner-And-Launcher-Replacer.bat --revert`

### macOS

Double-click `mac/Firestick-Cleaner-And-Launcher-Replacer.command` in Finder. On first run, macOS Gatekeeper may block it — right-click → **Open** → confirm. If the script complains that `adb` won't run, clear quarantine on the platform-tools folder:

```
xattr -dr com.apple.quarantine mac/adb
```

To revert: `./Firestick-Cleaner-And-Launcher-Replacer.command --revert`

### Linux

From the `linux/` directory:

```
chmod +x Firestick-Cleaner-And-Launcher-Replacer.sh adb/adb
./Firestick-Cleaner-And-Launcher-Replacer.sh
```

To revert: `./Firestick-Cleaner-And-Launcher-Replacer.sh --revert`

### Revert behavior

The revert flag re-enables all disabled packages, removes Projectivy, and restores all settings to stock.

## What Gets Disabled

The script splits packages into two categories:

- **Always disabled** — Tracking, telemetry, ads, and background services. These are always removed with no prompt.
- **Optional (Y/N prompt)** — Apps you might actually use (Amazon Music, Photos, Appstore, etc.). The script asks you for each one before disabling.

Some packages may be protected by Amazon on certain firmware versions — the script handles these gracefully and reports them.

### Ads, Tracking & Telemetry

| Package | What It Does |
|---------|-------------|
| `com.amazon.tv.acr` | **Automatic Content Recognition** — tracks what you watch on screen, even non-Amazon content |
| `com.amazon.hybridadidservice` | **Advertising ID Service** — provides a unique ad tracking identifier for targeted ads |
| `com.amazon.perfc` | **Performance Collection** — collects device performance data |
| `com.amazon.perfcollection` | **Performance Collection Service** — additional performance telemetry |
| `com.amazon.device.telemetry.emitter` | **Telemetry Emitter** — sends device telemetry data to Amazon servers |
| `com.amazon.wirelessmetrics.service` | **Wireless Metrics** — collects WiFi/Bluetooth usage data |

### Optional Apps (Y/N Prompt)

The script asks you whether to disable each of these — answer N to keep ones you use:

| Package | What It Does |
|---------|-------------|
| **Amazon Video services** (group) | **Prime Video app + DRM / playback stack** — disables all 8 packages below. Answer **N** if you use Prime Video, Freevee, or MiniTV, or those apps will not open or play. |
| `com.amazon.firebat` | **Prime Video** — the Prime Video app itself |
| `com.amazon.avls.experience` | **AVLS** — Amazon Video Licensing Service (DRM) |
| `com.amazon.prism.android.service` | **Prism** — Amazon's media playback framework |
| `com.amazon.dp.logger` | **Digital Products Logger** — required by Prime Video for telemetry |
| `com.amazon.livedeviceservice` | **Live Device Service** — live/streaming device coordination |
| `com.amazon.rtcsessioncontroller` | **RTC Session Controller** — streaming session manager |
| `com.amazon.client.metrics.api` | **Metrics API** — required for many Amazon apps to start |
| `com.amazon.tv.ottssocompanionapp` | **Amazon sign-in for apps** — single sign-on helper |
| `com.amazon.venezia` | **Amazon Appstore** — Amazon's app store (you can sideload apps instead) |
| `com.amazon.bueller.photos` | **Amazon Photos** — cloud photo app |
| `com.amazon.bueller.music` | **Amazon Music** — music streaming app |
| `com.amazon.imdb.tv.android.app` | **Freevee / IMDb TV** — Amazon's ad-supported streaming service |
| `com.amazon.minitv.android.app` | **MiniTV** — Amazon's short-form video service |
| `com.amazon.gamehub` | **Game Hub** — Amazon's game discovery service |
| `com.amazon.tv.livetv` | **Live TV** — Amazon's live TV integration |
| `com.amazon.franktvinput` | **Live TV input** — feeds the Live tab |
| `com.amazon.tifobserver` | **TV input observer** — watches the TV input framework for the Live tab |
| `com.amazon.tv.alexaalerts` | **Alexa Alerts** — Alexa notification alerts on screen |
| `com.amazon.tv.alexanotifications` | **Alexa Notifications** — Alexa push notifications |
| `com.amazon.audiohome` | **Audio Home** — Alexa audio/smart home dashboard |
| **Alexa extras** (group) | Hints, multi-room audio and Alexa music playback. The voice engine (`com.amazon.vizzini`) stays, so voice search on the remote keeps working. |
| `com.amazon.aca` | **Alexa hints** — on-screen Alexa cues (~44 MB resident) |
| `com.amazon.aiondec` | **Alexa skill connections** |
| `com.amazon.alexa.datastore.app` | **Alexa data store** |
| `com.amazon.alexadirectivebrokerservice` | **Alexa directive broker** |
| `com.amazon.alexa.externalmediaplayer.fireos` | **Alexa music playback** |
| `com.amazon.alexamediaplayer.runtime.ftv` | **Alexa media player runtime** |
| `com.amazon.whad`, `com.amazon.whasettings`, `com.amazon.wha.mediabrowserservice` | **Whole-home audio** — multi-room music |
| `com.amazon.spotify.mediabrowserservice` | **Spotify for Alexa** — stub only; the Spotify app is separate |
| `com.amazon.cloud9` | **Silk Browser** — Amazon's web browser |
| `com.amazon.smarthomemapviewapp` | **Smart Home Map** — Alexa smart home floor plan view |
| `com.amazon.tv.matter`, `com.amazon.tv.mattercohost` | **Matter** — Matter casting and smart-home controller |
| `com.amazon.whisperplay.service.install` | **WhisperPlay** — second-screen/casting service |
| `com.amazon.awvflingreceiver`, `com.amazon.cast.sink`, `com.amazon.dialservice` | **Amazon casting** — fling, cast sink and DIAL receivers. Casting through third-party apps still works. |
| `com.amazon.tcomm.jackson` | **tcomm support library** — used by Amazon's casting/messaging stack |

### Shopping (Always Disabled)

| Package | What It Does |
|---------|-------------|
| `com.amazon.shoptv.client` | **Shop from TV** — Amazon shopping integration |
| `com.amazon.shoptv.firetv.client` | **Shop from Fire TV** — Fire TV-specific shopping overlay |

### Screensaver & UI Bloat

| Package | What It Does |
|---------|-------------|
| `com.amazon.ftv.screensaver` | **Screensaver** — ad-filled screensaver that displays Amazon promotions |
| `com.amazon.tv.ftvambient` | **Ambient Mode** — ambient display mode with Amazon content |
| `com.amazon.sneakpeek` | **Sneak Peek** — preview/promotional content popups |
| `com.amazon.tv.turnstile` | **Content Suggestions** — "recommended" content that's mostly ads |
| `com.amazon.tv.notificationcenter` | **Notification Center** — Amazon promotional notifications |
| `com.amazon.tv.releasenotes` | **Release Notes** — "what's new" screens after updates |
| `com.amazon.storm.lightning.tutorial` | **Setup Tutorial** — first-run tutorial (not needed after setup) |
| `com.amazon.tmm.tutorial` | **Tutorial Manager** — additional tutorial/walkthrough screens |

### Amazon Background Services (Always Disabled)

| Package | What It Does |
|---------|-------------|
| `com.amazon.fireos.cirruscloud` | **Cirrus Cloud** — Amazon cloud sync service |
| `com.amazon.ods.kindleconnect` | **Kindle Connect** — Kindle device linking |
| `com.amazon.tahoe` | **Amazon Kids** — kids profiles (formerly FreeTime) |
| `com.amazon.aria` | **Aria** — Alexa surfaces and voice search screens |
| `com.amazon.hedwig` | **Fire TV Channels** — free ad-supported channels row |
| `com.amazon.tv.support` | **TV Support** — Amazon remote support/diagnostics tool |
| `com.amazon.ceviche` | **Ceviche** — Amazon A/B testing and experimentation framework |
| `com.amazon.d3` | **D3** — Amazon device data service |
| `com.amazon.device.rdmapplication` | **Remote Device Management** — allows Amazon to remotely manage your device |
| `com.amazon.wifilocker` | **WiFi Locker** — shares your WiFi password with Amazon |
| `com.amazon.spiderpork` | **Amazon Luna** — cloud gaming service |
| `com.amazon.ssm` | **SSM** — System Status Monitor |
| `com.amazon.ssmsys` | **SSM System** — System Status Monitor system service |
| `com.amazon.tv.easyupgrade` | **Easy Upgrade** — prompts to upgrade to newer Fire TV devices |
| `com.amazon.dpcclient` | **DPC Client** — device policy controller for Amazon management |
| `com.amazon.sharingservice.android.client.proxy` | **Sharing Service** — Amazon cross-device sharing |
| `com.amazon.privacypassservice` | **Privacy Pass** — Amazon privacy token service |
| `com.amazon.tv.legal.notices` | **Legal Notices** — legal disclaimer screens |
| `com.amazon.tv.legalresources` | **Legal Resources** — more legal screens |

### Ads, Setup Helpers & Leftovers (Always Disabled)

These come from [CleanYourTV](https://github.com/Gathin23/cleanyourtv)'s tested Fire TV Stick lists. Packages missing on your model are skipped.

| Package | What It Does |
|---------|-------------|
| `com.amazon.kso.blackbird` | **Ad bidding** — launcher ad sync and bidding |
| `com.amazon.tv.developer.dataservice` | **Personalization data** — shares content personalization data with third-party apps |
| `com.amazon.media.recommendations` | **Recommendations** — sponsored and recommendation rows |
| `com.amazon.uxnotification` | **UX notifications** — popup notifications |
| `com.amazon.systemnotices` | **System notices** |
| `com.amazon.device.sale.service` | **Sales** — sale and deal campaign service |
| `com.amazon.neo.minerva`, `com.amazon.neo.minerva.logcat`, `com.amazon.tv.fw.metrics` | **Metrics and logging** |
| `com.amazon.tv.website_launcher` | **Website launcher** — opens websites in Silk |
| `com.amazon.tv.ffsprovisioneeclient` | **Frustration-free setup client** |
| `com.amazon.whisperjoin.middleware.np`, `com.amazon.whisperjoin.middleware.v2.np` | **Frustration-free setup** of other Amazon devices |
| `com.amazon.dummy.*` (alarmclock, calendar, contacts, gallery, music, soundpicker) | **Placeholder apps** — empty stand-ins for phone apps |
| `com.android.*` leftovers (cts shims, protips, dreams.basic, print services, bookmarkprovider, calllogbackup, smspush, mms.service, hotspot2.osulogin, traceur, onetimeinitializer, carrierconfig, safetyregulatoryinfo, htmlviewer) | **Unused Android components** — phone features with no use on a stick |

### Left Enabled on Purpose

| Package | Why |
|---------|-----|
| `com.amazon.logan` | **Accessibility and text-to-speech** — CleanYourTV keeps it enabled in every Fire TV profile |
| `com.amazon.imp` | Needed for Amazon's home screen to render, and Settings lives in the Amazon launcher |
| `com.amazon.vizzini` | Alexa voice engine, so voice search on the remote keeps working |
| `com.amazon.dummy.settings` | Left alone because Settings already misbehaves with the Home redirect (see Notes) |

### Protected by Amazon (Cannot Disable Without Root)

These packages are protected at the system level. The script detects and skips them automatically:

| Package | What It Does |
|---------|-------------|
| `com.amazon.tv.launcher` | **Stock Launcher** — Amazon's home screen (runs in background ~110 MB) |
| `com.amazon.device.software.ota` | **OTA Updates** — Fire OS update service |
| `com.amazon.device.metrics` | **Core Metrics** — system-level metrics collection |
| `com.amazon.client.metrics` | **Client Metrics** — client-side metrics framework |
| `com.amazon.ftvads.deeplinking` | **Ad Deeplinking** — routes ad clicks to content |
| `com.amazon.tv.forcedotaupdater.v2` | **Forced OTA Updater** — forces system updates |
| `com.amazon.device.software.ota.override` | **OTA Override** — override mechanism for updates |
| `com.amazon.device.logmanager` | **Log Manager** — system log management |
| `com.amazon.device.sync` | **Device Sync** — syncs device state with Amazon |
| `com.amazon.securitysyncclient` | **Security Sync** — security policy sync service |
| `com.amazon.tv.csapp` | **Customer Service App** — Amazon support app |
| `com.amazon.tv.oobe` | **Out of Box Experience** — initial setup wizard |

## How It Works

Amazon protects the stock launcher as a system package, so it cannot be disabled or uninstalled without root. Instead, Projectivy uses an **Android Accessibility Service** to intercept the Home button press and redirect it to Projectivy's home screen.

The script:
1. Enables `ProjectivyAccessibilityService` via `settings put secure`
2. Assigns the `android.app.role.HOME` role to Projectivy
3. Grants overlay (`SYSTEM_ALERT_WINDOW`) and notification listener permissions, and exempts Projectivy from battery optimization (`deviceidle whitelist`) so Fire OS doesn't put it to sleep
4. Disables bloatware in a loop — if some packages are protected, it reboots and retries in case new ones become available
5. After rebooting, re-applies any disables that Amazon reverted during the reboot

After installation, you need to configure Projectivy on the TV:
1. Open Projectivy **Settings** (long-press the center/select button on the remote)
2. Go to **General**
3. Enable **Override current launcher**

This ensures the Home button always opens Projectivy instead of the Amazon launcher.

## Project Structure

```
Firestick-Cleanup/
├── README.md
├── screenshot.jpg
├── windows/
│   ├── Firestick-Cleaner-And-Launcher-Replacer.bat   # Console version
│   ├── adb/                                          # Bundled Windows ADB
│   └── WinformVer/                                   # WinForms GUI source
├── mac/
│   ├── Firestick-Cleaner-And-Launcher-Replacer.command
│   └── adb/                                          # Bundled macOS ADB
└── linux/
    ├── Firestick-Cleaner-And-Launcher-Replacer.sh
    └── adb/                                          # Bundled Linux ADB
```

## Notes

- **Fire TV updates** may reset these changes. Consider blocking OTA updates at the router level by blocking `softwareupdates.amazon.com` and `fireos-fireos-update.s3.amazonaws.com`.
- All changes are **fully reversible** — run with `--revert` to undo everything.
- The stock Amazon launcher remains running in the background (~110 MB RAM) since Amazon protects it from being disabled. This is unavoidable without root.
- Settings persist through reboots. The script verifies this automatically.
- **Appstore updates can re-enable apps.** An app you disabled can come back on when the Appstore updates it. Check `adb shell pm list packages -d` now and then, or run the script again.
- **Fire TV Settings with the Home override on:** Settings lives inside Amazon's launcher, so with Projectivy's Home override active, opening Settings bounces back to Projectivy. This is a Fire OS limitation and also affects Home on Fire ([issue #12](https://github.com/toolicious/home-on-fire/issues/12)). To use Settings, temporarily turn off the accessibility service: `adb shell settings put secure enabled_accessibility_services '""'`, then re-run the enable step when you're done.
- **Privacy toggles** (Device Usage Data, Collect App Usage Data, Interest-based Ads, Featured Content autoplay) sync with your Amazon account, so set them in Settings > Preferences > Privacy Settings rather than with `adb`.
- Tested on Fire TV Stick 4K and Fire TV Stick Lite. The packages added in the Ads, Setup Helpers & Leftovers and Alexa extras groups were applied and checked across a reboot on a Fire TV Stick 4K Max 2nd gen (AFTKRT, Fire OS 8.1.7.4).

## License

MIT
