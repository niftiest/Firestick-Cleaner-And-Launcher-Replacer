using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FirestickCleanup
{
    public partial class MainForm : Form
    {
        private const string PROJECTIVY_PKG = "com.spocky.projengmenu";
        private const string PROJECTIVY_ACTIVITY = PROJECTIVY_PKG + "/.ui.home.MainActivity";
        private const string PROJECTIVY_ACCESSIBILITY = PROJECTIVY_PKG + "/.services.ProjectivyAccessibilityService";
        private const string PROJECTIVY_NOTIFICATION = PROJECTIVY_PKG + "/.services.notification.NotificationListener";
        private const string FALLBACK_APK_URL = "https://github.com/spocky/miproja1/releases/download/4.68/ProjectivyLauncher-4.68-c82-xda-release.apk";

        private static readonly string[] AlwaysDisabled =
        {
            "com.amazon.tv.acr", "com.amazon.hybridadidservice", "com.amazon.perfc", "com.amazon.perfcollection",
            "com.amazon.device.telemetry.emitter", "com.amazon.wirelessmetrics.service",
            "com.amazon.shoptv.client", "com.amazon.shoptv.firetv.client", "com.amazon.sneakpeek",
            "com.amazon.ftv.screensaver", "com.amazon.storm.lightning.tutorial", "com.amazon.tmm.tutorial",
            "com.amazon.tv.releasenotes", "com.amazon.device.rdmapplication",
            "com.amazon.fireos.cirruscloud", "com.amazon.ods.kindleconnect", "com.amazon.tahoe",
            "com.amazon.aria", "com.amazon.hedwig", "com.amazon.tv.support", "com.amazon.ceviche",
            "com.amazon.d3", "com.amazon.tv.turnstile", "com.amazon.tv.ftvambient", "com.amazon.wifilocker",
            "com.amazon.spiderpork", "com.amazon.tv.notificationcenter",
            "com.amazon.ssm", "com.amazon.ssmsys", "com.amazon.tv.easyupgrade", "com.amazon.dpcclient",
            "com.amazon.sharingservice.android.client.proxy", "com.amazon.privacypassservice",
            "com.amazon.tv.legal.notices",
            // Ads, telemetry and setup helpers (from CleanYourTV's tested Fire TV Stick lists)
            "com.amazon.kso.blackbird", "com.amazon.tv.developer.dataservice", "com.amazon.media.recommendations",
            "com.amazon.uxnotification", "com.amazon.systemnotices", "com.amazon.tv.website_launcher",
            "com.amazon.tv.ffsprovisioneeclient", "com.amazon.whisperjoin.middleware.np",
            "com.amazon.whisperjoin.middleware.v2.np", "com.amazon.tv.legalresources", "com.amazon.device.sale.service",
            "com.amazon.neo.minerva", "com.amazon.neo.minerva.logcat", "com.amazon.tv.fw.metrics",
            // Dummy apps and unused Android components
            "com.amazon.dummy.alarmclock", "com.amazon.dummy.calendar", "com.amazon.dummy.contacts",
            "com.amazon.dummy.gallery", "com.amazon.dummy.music", "com.amazon.dummy.soundpicker",
            "com.android.cts.ctsshim", "com.android.cts.priv.ctsshim", "com.android.protips", "com.android.dreams.basic",
            "com.android.printspooler", "com.android.bips", "com.android.printservice.recommendation",
            "com.android.bookmarkprovider", "com.android.calllogbackup", "com.android.smspush", "com.android.mms.service",
            "com.android.hotspot2.osulogin", "com.android.traceur", "com.android.onetimeinitializer",
            "com.android.carrierconfig", "com.android.safetyregulatoryinfo", "com.android.htmlviewer"
        };

        private static readonly string[] VideoServices =
        {
            "com.amazon.avls.experience", "com.amazon.prism.android.service", "com.amazon.dp.logger",
            "com.amazon.livedeviceservice", "com.amazon.rtcsessioncontroller", "com.amazon.client.metrics.api",
            "com.amazon.firebat", // the Prime Video app itself
            "com.amazon.tv.ottssocompanionapp"
        };

        private static readonly string[] AlexaExtras =
        {
            "com.amazon.aca", "com.amazon.aiondec", "com.amazon.alexa.datastore.app",
            "com.amazon.alexadirectivebrokerservice", "com.amazon.alexa.externalmediaplayer.fireos",
            "com.amazon.alexamediaplayer.runtime.ftv", "com.amazon.whad", "com.amazon.whasettings",
            "com.amazon.wha.mediabrowserservice", "com.amazon.spotify.mediabrowserservice"
        };

        private readonly string adbPath;
        private readonly string logFile;

        public MainForm()
        {
            InitializeComponent();
            adbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb", "adb.exe");
            logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firestick_cleanup.log");
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string ip = txtDeviceIP.Text.Trim();
            if (!IsValidIP(ip))
            {
                MessageBox.Show("Enter a valid IPv4 address (e.g. 10.0.0.20).", "Invalid IP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!File.Exists(adbPath))
            {
                MessageBox.Show($"ADB not found at:\n{adbPath}", "ADB missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ResetLog();
            SetButtons(false);
            try
            {
                await Task.Run(() => RunCleanupAsync(ip));
            }
            catch (Exception ex)
            {
                Log("[!!] " + ex.Message);
                SetStatus("Error: " + ex.Message);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetButtons(true);
            }
        }

        private async void BtnRevert_Click(object sender, EventArgs e)
        {
            string ip = txtDeviceIP.Text.Trim();
            if (!IsValidIP(ip))
            {
                MessageBox.Show("Enter a valid IPv4 address (e.g. 10.0.0.20).", "Invalid IP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!File.Exists(adbPath))
            {
                MessageBox.Show($"ADB not found at:\n{adbPath}", "ADB missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (MessageBox.Show("Re-enable all disabled packages, remove Projectivy, and restore stock settings?",
                    "Revert", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;

            ResetLog();
            SetButtons(false);
            try
            {
                await Task.Run(() => RunRevertAsync(ip));
            }
            catch (Exception ex)
            {
                Log("[!!] " + ex.Message);
                SetStatus("Error: " + ex.Message);
            }
            finally
            {
                SetButtons(true);
            }
        }

        private async Task RunCleanupAsync(string ip)
        {
            SetProgress(0, "Connecting...");
            await RunAdb($"disconnect");
            if (!await ConnectAsync(ip)) return;

            SetProgress(10, "Capturing baseline RAM...");
            var (totalBefore, availBefore, procsBefore) = await CaptureMemAsync(ip);
            Log($"  Total RAM: {totalBefore} MB | Available: {availBefore} MB | Amazon procs: {procsBefore}");

            SetProgress(20, "Downloading Projectivy...");
            string apkLocal = await DownloadProjectivyAsync();
            if (apkLocal == null) { Log("[!!] Download failed."); SetStatus("Download failed."); return; }

            SetProgress(35, "Installing Projectivy...");
            if (!await InstallProjectivyAsync(ip, apkLocal)) { SetStatus("Install failed."); return; }

            SetProgress(50, "Configuring Projectivy as launcher...");
            await ConfigureProjectivyAsync(ip);

            ShowMessage(
                "On your Fire TV:\n\n" +
                "1. Projectivy should now be on your screen\n" +
                "2. Open Projectivy Settings (long-press center button)\n" +
                "3. Select 'General'\n" +
                "4. Enable 'Override current launcher'\n\n" +
                "Click OK here once done.",
                "Configure Projectivy");

            SetProgress(60, "Disabling bloatware...");
            int totalDisabled = await DisableBloatLoopAsync(ip, BuildBloatList());

            SetProgress(95, "Verifying...");
            await VerifyAsync(ip);

            var (totalAfter, availAfter, procsAfter) = await CaptureMemAsync(ip);
            int freed = availAfter - availBefore;
            int killed = procsBefore - procsAfter;

            Log("");
            Log("                    BEFORE      AFTER     CHANGE");
            Log("  ----------------------------------------------------");
            Log($"  Available RAM:    {availBefore,4} MB    {availAfter,4} MB   +{freed} MB freed");
            Log($"  Amazon processes: {procsBefore,4}        {procsAfter,4}       -{killed} removed");
            Log("");
            Log($"  ALL DONE — {totalDisabled} packages disabled, {freed} MB freed.");

            SetProgress(100, $"Done — {totalDisabled} packages disabled, {freed} MB freed.");
            try { File.Delete(apkLocal); } catch { }
        }

        private async Task RunRevertAsync(string ip)
        {
            SetProgress(0, "Connecting...");
            if (!await ConnectAsync(ip)) return;

            SetProgress(20, "Re-enabling disabled packages...");
            var (_, listOut) = await RunAdb($"-s {ip}:5555 shell pm list packages -d");
            var disabled = Regex.Matches(listOut ?? "", @"package:(\S+)")
                .Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            int reEnabled = 0;
            foreach (var pkg in disabled)
            {
                await RunAdb($"-s {ip}:5555 shell pm enable {pkg}");
                Log($"  [OK] Enabled: {pkg}");
                reEnabled++;
            }

            SetProgress(60, "Removing Projectivy launcher settings...");
            await RunAdb($"-s {ip}:5555 shell settings put secure enabled_accessibility_services \"\"");
            await RunAdb($"-s {ip}:5555 shell settings put secure accessibility_enabled 0");
            await RunAdb($"-s {ip}:5555 shell settings put secure enabled_notification_listeners \"\"");
            await RunAdb($"-s {ip}:5555 shell appops set {PROJECTIVY_PKG} SYSTEM_ALERT_WINDOW deny");
            await RunAdb($"-s {ip}:5555 shell cmd role remove-role-holder android.app.role.HOME {PROJECTIVY_PKG}");

            SetProgress(85, "Uninstalling Projectivy...");
            var (_, uninst) = await RunAdb($"-s {ip}:5555 shell pm uninstall {PROJECTIVY_PKG}");
            Log(uninst.Contains("Success") ? "  [OK] Projectivy uninstalled" : "  [--] Projectivy was not installed");

            SetProgress(100, $"Revert complete — {reEnabled} packages re-enabled.");
            Log($"  ALL DONE — {reEnabled} packages re-enabled.");
        }

        private async Task<bool> ConnectAsync(string ip)
        {
            await RunAdb($"connect {ip}:5555");
            ShowMessage(
                "On your Fire TV: select 'Always allow from this computer' and press OK.\n\n" +
                "Click OK here once you've authorized the connection.",
                "Authorize ADB");
            await RunAdb($"connect {ip}:5555");
            await Task.Delay(2000);

            var (_, output) = await RunAdb($"-s {ip}:5555 shell echo ok");
            if (output.Contains("ok"))
            {
                Log($"  [OK] Connected to {ip}:5555");
                return true;
            }
            Log("  [!!] Could not connect. Check that ADB Debugging is enabled and that you authorized the connection.");
            SetStatus("Connection failed.");
            return false;
        }

        private async Task<(int total, int avail, int procs)> CaptureMemAsync(string ip)
        {
            var (_, total) = await RunAdb($"-s {ip}:5555 shell \"cat /proc/meminfo | grep MemTotal\"");
            var (_, avail) = await RunAdb($"-s {ip}:5555 shell \"cat /proc/meminfo | grep MemAvailable\"");
            var (_, procs) = await RunAdb($"-s {ip}:5555 shell \"ps -A | grep com.amazon | wc -l\"");

            int totalMB = ParseKbToMb(total);
            int availMB = ParseKbToMb(avail);
            int procCount = int.TryParse((procs ?? "0").Trim(), out var n) ? n : 0;
            return (totalMB, availMB, procCount);
        }

        private async Task<string> DownloadProjectivyAsync()
        {
            string apkPath = Path.Combine(Path.GetTempPath(), "ProjectivyLauncher-latest.apk");
            string apkUrl = FALLBACK_APK_URL;

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("Firestick-Cleanup/1.4");
                    string json = await http.GetStringAsync("https://api.github.com/repos/spocky/miproja1/releases/latest");
                    var m = Regex.Match(json, @"""browser_download_url"":\s*""([^""]+\.apk)""");
                    if (m.Success) apkUrl = m.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                Log($"  [..] Could not query GitHub API ({ex.Message}). Using fallback URL.");
            }

            Log($"  [..] Downloading: {apkUrl}");
            try
            {
                using (var http = new HttpClient())
                using (var resp = await http.GetAsync(apkUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    resp.EnsureSuccessStatusCode();
                    using (var src = await resp.Content.ReadAsStreamAsync())
                    using (var dst = File.Create(apkPath))
                    {
                        await src.CopyToAsync(dst);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"  [!!] Download failed: {ex.Message}");
                return null;
            }

            var fi = new FileInfo(apkPath);
            if (!fi.Exists || fi.Length < 100000)
            {
                Log("  [!!] Downloaded file is too small.");
                return null;
            }
            Log($"  [OK] Downloaded ({fi.Length / 1048576} MB)");
            return apkPath;
        }

        private async Task<bool> InstallProjectivyAsync(string ip, string apkLocal)
        {
            Log("  [..] Pushing APK to device...");
            var (_, push) = await RunAdb($"-s {ip}:5555 push \"{apkLocal}\" /data/local/tmp/projectivy.apk");
            if (!push.Contains("pushed"))
            {
                Log("  [!!] Failed to push APK.");
                Log(push);
                return false;
            }
            Log("  [OK] APK pushed");

            Log("  [..] Installing...");
            var (_, inst) = await RunAdb($"-s {ip}:5555 shell pm install -r /data/local/tmp/projectivy.apk");
            if (!inst.Contains("Success"))
            {
                Log("  [!!] Install failed.");
                Log(inst);
                return false;
            }

            var (_, path) = await RunAdb($"-s {ip}:5555 shell pm path {PROJECTIVY_PKG}");
            if (!path.Contains("package:"))
            {
                Log("  [!!] Install reported success but package not found on device.");
                return false;
            }
            Log("  [OK] Projectivy installed and verified");

            await RunAdb($"-s {ip}:5555 shell rm /data/local/tmp/projectivy.apk");
            return true;
        }

        private async Task ConfigureProjectivyAsync(string ip)
        {
            await RunAdb($"-s {ip}:5555 shell settings put secure enabled_accessibility_services {PROJECTIVY_ACCESSIBILITY}");
            await RunAdb($"-s {ip}:5555 shell settings put secure accessibility_enabled 1");
            var (_, accCheck) = await RunAdb($"-s {ip}:5555 shell settings get secure enabled_accessibility_services");
            Log(accCheck.Contains(PROJECTIVY_PKG) ? "  [OK] Accessibility service enabled" : "  [!!] Accessibility service not enabled");

            await RunAdb($"-s {ip}:5555 shell settings put secure enabled_notification_listeners {PROJECTIVY_NOTIFICATION}");
            Log("  [OK] Notification listener enabled");

            await RunAdb($"-s {ip}:5555 shell appops set {PROJECTIVY_PKG} SYSTEM_ALERT_WINDOW allow");
            await RunAdb($"-s {ip}:5555 shell dumpsys deviceidle whitelist +{PROJECTIVY_PKG}");
            Log("  [OK] Overlay permission granted");

            await RunAdb($"-s {ip}:5555 shell cmd role add-role-holder android.app.role.HOME {PROJECTIVY_PKG}");
            Log("  [OK] HOME role assigned");

            var (_, launch) = await RunAdb($"-s {ip}:5555 shell am start -n {PROJECTIVY_ACTIVITY}");
            if (launch.Contains("Error"))
            {
                Log("  [..] Direct launch failed, trying monkey...");
                var (_, monkey) = await RunAdb($"-s {ip}:5555 shell monkey -p {PROJECTIVY_PKG} -c android.intent.category.LEANBACK_LAUNCHER 1");
                Log(monkey.Contains("Events injected") ? "  [OK] Projectivy launched via fallback" : "  [!!] Could not auto-launch — open it manually on the TV");
            }
            else Log("  [OK] Projectivy launched");
        }

        private List<string> BuildBloatList()
        {
            var list = new List<string>(AlwaysDisabled);
            if (chkVideoServices.Checked) list.AddRange(VideoServices);
            if (chkAppstore.Checked) list.Add("com.amazon.venezia");
            if (chkPhotos.Checked) list.Add("com.amazon.bueller.photos");
            if (chkMusic.Checked) list.Add("com.amazon.bueller.music");
            if (chkFreevee.Checked) list.Add("com.amazon.imdb.tv.android.app");
            if (chkMinitv.Checked) list.Add("com.amazon.minitv.android.app");
            if (chkGames.Checked) list.Add("com.amazon.gamehub");
            if (chkLivetv.Checked) list.AddRange(new[] { "com.amazon.tv.livetv", "com.amazon.franktvinput", "com.amazon.tifobserver" });
            if (chkAlexa.Checked) { list.Add("com.amazon.tv.alexaalerts"); list.Add("com.amazon.tv.alexanotifications"); list.Add("com.amazon.audiohome"); }
            if (chkAlexaExtras.Checked) list.AddRange(AlexaExtras);
            if (chkSilk.Checked) list.Add("com.amazon.cloud9");
            if (chkSmarthome.Checked) list.AddRange(new[] { "com.amazon.smarthomemapviewapp", "com.amazon.tv.matter", "com.amazon.tv.mattercohost" });
            if (chkCast.Checked) list.AddRange(new[] { "com.amazon.whisperplay.service.install", "com.amazon.awvflingreceiver", "com.amazon.cast.sink", "com.amazon.dialservice", "com.amazon.tcomm.jackson" });
            return list;
        }

        private async Task<int> DisableBloatLoopAsync(string ip, List<string> bloat)
        {
            var done = new HashSet<string>();
            int totalDisabled = 0;
            int pass = 0;

            while (true)
            {
                pass++;
                Log("");
                Log($"=== Disable pass {pass} ===");
                int passDisabled = 0, passProtected = 0;

                foreach (var pkg in bloat)
                {
                    if (done.Contains(pkg)) continue;
                    var (_, output) = await RunAdb($"-s {ip}:5555 shell pm disable-user --user 0 {pkg}");
                    if (output.Contains("disabled-user"))
                    {
                        Log($"  [OK] Disabled: {pkg}");
                        done.Add(pkg);
                        passDisabled++;
                        totalDisabled++;
                    }
                    else if (output.Contains("SecurityException"))
                    {
                        Log($"  [--] Protected: {pkg}");
                        passProtected++;
                    }
                    else
                    {
                        Log($"  [--] Skipped: {pkg} (not found)");
                        done.Add(pkg);
                    }
                }
                Log($"  Pass {pass}: disabled {passDisabled}, protected {passProtected}");

                bool needRetry = passDisabled > 0 && passProtected > 0 && pass < 3;
                if (!needRetry) break;

                Log("  [..] Some protected — rebooting to retry...");
                if (!await RebootAndReconnectAsync(ip))
                {
                    Log("  [!!] Could not reconnect. Continuing.");
                    break;
                }
            }

            if (pass == 1)
            {
                Log("");
                Log("[..] Final reboot to apply changes...");
                if (!await RebootAndReconnectAsync(ip))
                {
                    Log("  [!!] Could not reconnect after reboot.");
                    return totalDisabled;
                }
            }

            Log("[..] Re-applying any packages Amazon re-enabled during reboot...");
            var (_, listOut) = await RunAdb($"-s {ip}:5555 shell pm list packages -d");
            int reapplied = 0;
            foreach (var pkg in done)
            {
                if (!listOut.Contains($"package:{pkg}"))
                {
                    var (_, retryOut) = await RunAdb($"-s {ip}:5555 shell pm disable-user --user 0 {pkg}");
                    if (retryOut.Contains("disabled-user")) reapplied++;
                }
            }
            Log(reapplied > 0 ? $"  [OK] Re-disabled {reapplied} packages" : "  [OK] All packages stayed disabled");
            return totalDisabled;
        }

        private async Task<bool> RebootAndReconnectAsync(string ip)
        {
            await RunAdb($"-s {ip}:5555 shell reboot");
            await Task.Delay(35_000);
            for (int i = 1; i <= 20; i++)
            {
                await RunAdb($"connect {ip}:5555");
                var (_, output) = await RunAdb($"-s {ip}:5555 shell echo ok");
                if (output.Contains("ok"))
                {
                    await Task.Delay(5_000);
                    Log("  [OK] Device back online");
                    return true;
                }
                Log($"  [..] Waiting for reboot... ({i}/20)");
                await Task.Delay(3_000);
            }
            return false;
        }

        private async Task VerifyAsync(string ip)
        {
            var (_, pkgs) = await RunAdb($"-s {ip}:5555 shell pm list packages");
            Log(pkgs.Contains(PROJECTIVY_PKG) ? "  [OK] Projectivy is installed" : "  [!!] Projectivy is NOT installed");

            var (_, acc) = await RunAdb($"-s {ip}:5555 shell settings get secure enabled_accessibility_services");
            Log(acc.Contains(PROJECTIVY_PKG) ? "  [OK] Accessibility service active" : "  [!!] Accessibility service NOT active");

            var (_, disabled) = await RunAdb($"-s {ip}:5555 shell pm list packages -d");
            int count = Regex.Matches(disabled ?? "", @"package:").Count;
            Log($"  [OK] {count} packages remain disabled");
        }

        private async Task<(int code, string output)> RunAdb(string args, int timeoutMs = 60000)
        {
            File.AppendAllText(logFile, $"[adb] {args}{Environment.NewLine}");
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = adbPath,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    };
                    using (var p = Process.Start(psi))
                    {
                        var stdout = p.StandardOutput.ReadToEndAsync();
                        var stderr = p.StandardError.ReadToEndAsync();
                        if (!p.WaitForExit(timeoutMs))
                        {
                            try { p.Kill(); } catch { }
                            return (-1, "[timeout]");
                        }
                        Task.WaitAll(stdout, stderr);
                        string combined = (stdout.Result ?? "") + (stderr.Result ?? "");
                        File.AppendAllText(logFile, combined + Environment.NewLine);
                        return (p.ExitCode, combined);
                    }
                }
                catch (Exception ex)
                {
                    return (-1, ex.Message);
                }
            });
        }

        private static int ParseKbToMb(string memInfoLine)
        {
            if (string.IsNullOrEmpty(memInfoLine)) return 0;
            var m = Regex.Match(memInfoLine, @"(\d+)\s*kB");
            if (!m.Success) return 0;
            return int.TryParse(m.Groups[1].Value, out var kb) ? kb / 1024 : 0;
        }

        private static bool IsValidIP(string s) =>
            !string.IsNullOrWhiteSpace(s) && IPAddress.TryParse(s, out var ip) &&
            ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;

        private void Log(string text)
        {
            if (txtLog.InvokeRequired) { txtLog.BeginInvoke((Action)(() => Log(text))); return; }
            txtLog.AppendText(text + Environment.NewLine);
            try { File.AppendAllText(logFile, text + Environment.NewLine); } catch { }
        }

        private void SetStatus(string text)
        {
            if (lblStatus.InvokeRequired) { lblStatus.BeginInvoke((Action)(() => SetStatus(text))); return; }
            lblStatus.Text = text;
        }

        private void SetProgress(int value, string status)
        {
            if (progressBar.InvokeRequired) { progressBar.BeginInvoke((Action)(() => SetProgress(value, status))); return; }
            progressBar.Value = Math.Max(0, Math.Min(100, value));
            lblStatus.Text = status;
            Log($"[{value,3}%] {status}");
        }

        private void SetButtons(bool enabled)
        {
            if (btnStart.InvokeRequired) { btnStart.BeginInvoke((Action)(() => SetButtons(enabled))); return; }
            btnStart.Enabled = enabled;
            btnRevert.Enabled = enabled;
            txtDeviceIP.Enabled = enabled;
            grpOptions.Enabled = enabled;
        }

        private void ShowMessage(string text, string title)
        {
            if (InvokeRequired) { Invoke((Action)(() => ShowMessage(text, title))); return; }
            MessageBox.Show(this, text, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ResetLog()
        {
            if (txtLog.InvokeRequired) { txtLog.Invoke((Action)ResetLog); return; }
            txtLog.Clear();
            try
            {
                File.WriteAllText(logFile,
                    "============================================" + Environment.NewLine +
                    "  Firestick Cleanup Log" + Environment.NewLine +
                    "  Started: " + DateTime.Now + Environment.NewLine +
                    "============================================" + Environment.NewLine);
            }
            catch { }
        }
    }
}
