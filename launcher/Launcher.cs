using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed class Launcher : Form
{
    private const string PackageUrl = "https://thunderstore.io/package/download/BepInEx/BepInExPack_EtG/5.4.2101/";
    private const string PackageHash = "6b97edb311e961e78ca1cd913a886fd72b1ee936c73300c8992b8bf86ffde7ba";
    private TextBox gamePath;
    private Label status;
    private Button launch;
    private static string SettingsPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game-path.txt"); } }

    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--self-test") return SelfTest(args[1]);
        if (args.Length == 3 && args[0] == "--package-test") return PackageTest(args[1], args[2]);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Launcher()); return 0;
    }

    private Launcher()
    {
        Text = "Gungeon Together LAN — dungeon-кооператив";
        Width = 650; Height = 325; FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
        Label intro = new Label { Left = 18, Top = 18, Width = 600, Height = 56,
            Text = "Альфа 0.3.0: сглаживание движения, выбор героя и новое меню.\nSteam использует обычный профиль; этот запускатор — отдельный LAN-профиль.\nЛобби → Ctrl+P → создать / подключиться → хост начинает забег." };
        Controls.Add(intro);
        Controls.Add(new Label { Left = 18, Top = 88, Width = 600, Text = "Папка игры, в которой находится EtG.exe:" });
        gamePath = new TextBox { Left = 18, Top = 112, Width = 500 };
        if (File.Exists(SettingsPath)) gamePath.Text = File.ReadAllText(SettingsPath).Trim();
        Controls.Add(gamePath);
        Button browse = new Button { Left = 526, Top = 110, Width = 85, Text = "Обзор…" };
        browse.Click += delegate {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                dialog.Description = "Выберите папку Enter the Gungeon с EtG.exe";
                if (dialog.ShowDialog(this) == DialogResult.OK) gamePath.Text = dialog.SelectedPath;
            }
        };
        Controls.Add(browse);
        launch = new Button { Left = 18, Top = 155, Width = 593, Height = 42, Text = "Установить при необходимости и запустить игру" };
        launch.Click += async delegate { await InstallAndLaunch(); };
        Controls.Add(launch);
        status = new Label { Left = 18, Top = 210, Width = 593, Height = 65,
            Text = "Первый запуск скачает официальный BepInExPack EtG (~720 КБ), если его нет.\nИсходники и инструкция находятся рядом. Никаких отдельных серверов/скриптов." };
        Controls.Add(status);
    }

    private async Task InstallAndLaunch()
    {
        launch.Enabled = false;
        try {
            string root = Path.GetFullPath(gamePath.Text.Trim());
            if (!File.Exists(Path.Combine(root, "EtG.exe")) || !File.Exists(Path.Combine(root, "EtG_Data/Managed/Assembly-CSharp.dll")))
                throw new IOException("В этой папке не найдены EtG.exe и библиотеки игры.");
            foreach (Process process in Process.GetProcessesByName("EtG")) {
                process.Dispose(); throw new IOException("Закройте запущенную Enter the Gungeon перед установкой/запуском.");
            }
            string plugins = Path.Combine(root, "BepInEx/plugins");
            string target = Path.Combine(plugins, "GungeonTogetherLAN/GungeonTogether.LAN-0.3.0-alpha.dll");
            List<string> upgrades = new List<string>();
            if (Directory.Exists(plugins)) {
                foreach (string file in Directory.GetFiles(plugins, "*.dll", SearchOption.AllDirectories)) {
                    string filename = Path.GetFileName(file);
                    if (file.Equals(target, StringComparison.OrdinalIgnoreCase) && Hash(File.ReadAllBytes(file)) == Hash(ResourcePlugin())) continue;
                    if (filename == "GungeonTogether.LAN-0.1.0-beta.dll" || filename == "GungeonTogether.LAN-0.2.0-alpha.dll" || filename == "GungeonTogether.LAN-0.2.1-alpha.dll" || filename == "GungeonTogether.LAN-0.2.2-alpha.dll" || filename == "GungeonTogether.LAN-0.3.0-alpha.dll") { upgrades.Add(file); continue; }
                    if (filename.IndexOf("GungeonTogether", StringComparison.OrdinalIgnoreCase) >= 0)
                        throw new IOException("Уже установлен другой GungeonTogether:\n" + file + "\nУберите его из plugins перед установкой LAN-версии.");
                }
            }
            if (upgrades.Count != 0) {
                if (MessageBox.Show(this, "Найдены предыдущие LAN-DLL:\n" + string.Join("\n", upgrades.ToArray()) + "\n\nСохранить их в BepInEx/GungeonTogetherLAN/backups и установить альфу 0.3.0? Обычные сохранения игры не меняются.", "Обновление LAN-мода", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
                string backup = Path.Combine(root, "BepInEx/GungeonTogetherLAN/backups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(backup);
                for (int i = 0; i < upgrades.Count; i++) File.Move(upgrades[i], Path.Combine(backup, i + "-" + Path.GetFileName(upgrades[i]) + ".disabled"));
            }
            string core = Path.Combine(root, "BepInEx/core/BepInEx.dll");
            if (!File.Exists(core)) {
                if (MessageBox.Show(this, "Будут добавлены BepInExPack EtG 5.4.2101 и LAN-мод в выбранную папку игры.\nСуществующие отличающиеся файлы не перезаписываются. Продолжить?", "Первая установка", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
                status.Text = "Скачиваю официальный BepInExPack EtG…";
                string zip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInExPack_EtG-5.4.2101.download.zip");
                try {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using (WebClient web = new WebClient()) await web.DownloadFileTaskAsync(new Uri(PackageUrl), zip);
                    if (Hash(File.ReadAllBytes(zip)) != PackageHash) throw new IOException("Контрольная сумма BepInExPack не совпала. Установка остановлена.");
                    InstallPack(zip, root);
                } finally { if (File.Exists(zip)) File.Delete(zip); }
            } else if (AssemblyName.GetAssemblyName(core).Version.Major != 5) {
                throw new IOException("Нужен BepInEx 5 (BepInExPack EtG), найден другой выпуск.");
            }
            if (!File.Exists(Path.Combine(root, "winhttp.dll")) || !File.Exists(Path.Combine(root, "doorstop_config.ini")))
                throw new IOException("Не найдена обычная ручная установка BepInEx. Для профиля мод-менеджера используйте инструкцию README.ru.md.");
            byte[] plugin = ResourcePlugin();
            if (File.Exists(target) && Hash(File.ReadAllBytes(target)) != Hash(plugin))
                throw new IOException("В целевой папке уже находится отличающаяся LAN-DLL. Сохраните её отдельно и уберите перед установкой новой версии.");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (!File.Exists(target)) File.WriteAllBytes(target, plugin);
            if (Hash(File.ReadAllBytes(target)) != Hash(plugin)) throw new IOException("Проверка установленной DLL не прошла.");
            File.WriteAllText(SettingsPath, root, Encoding.UTF8);
            status.Text = "Мод установлен и проверен. Запускаю игру; меню Ctrl+P.";
            Process.Start(new ProcessStartInfo(Path.Combine(root, "EtG.exe"), "--gt-lan-profile") { WorkingDirectory = root, UseShellExecute = true });
            Close();
        } catch (Exception ex) {
            status.Text = "Остановлено: " + ex.Message;
            MessageBox.Show(this, ex.Message, "Не удалось запустить", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        } finally { if (!IsDisposed) launch.Enabled = true; }
    }

    private static byte[] ResourcePlugin()
    {
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LanPlugin"))
        using (MemoryStream copy = new MemoryStream()) { stream.CopyTo(copy); return copy.ToArray(); }
    }
    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    internal static string SafeTarget(string root, string relative)
    {
        string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string target = Path.GetFullPath(Path.Combine(prefix, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new IOException("Недопустимый путь в архиве.");
        return target;
    }

    internal static void InstallPack(string zip, string root)
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        using (ZipArchive archive = ZipFile.OpenRead(zip)) {
            foreach (ZipArchiveEntry entry in archive.Entries) {
                const string prefix = "BepInExPack_EtG/";
                if (!entry.FullName.StartsWith(prefix, StringComparison.Ordinal) || entry.FullName.EndsWith("/")) continue;
                string relative = entry.FullName.Substring(prefix.Length);
                string target = SafeTarget(root, relative);
                using (Stream input = entry.Open())
                using (MemoryStream buffer = new MemoryStream()) {
                    input.CopyTo(buffer); byte[] bytes = buffer.ToArray();
                    if (relative.Equals("BepInEx/config/BepInEx.cfg", StringComparison.OrdinalIgnoreCase)) {
                        // A fresh install needs no auxiliary console window.
                        string config = Encoding.UTF8.GetString(bytes);
                        config = Regex.Replace(config, @"(\[Logging\.Console\][\s\S]*?\r?\nEnabled\s*=\s*)true", "$1false");
                        bytes = Encoding.UTF8.GetBytes(config);
                    }
                    if (File.Exists(target)) {
                        if (relative.StartsWith("BepInEx/config/", StringComparison.OrdinalIgnoreCase)) continue;
                        if (Hash(File.ReadAllBytes(target)) != Hash(bytes)) throw new IOException("Отличающийся файл уже существует: " + target);
                    } else files.Add(target, bytes);
                }
            }
        }
        if (files.Count == 0 && !File.Exists(Path.Combine(root, "BepInEx/core/BepInEx.dll"))) throw new IOException("Пустой/неподходящий архив BepInEx.");
        // Validate every collision before writing any package files.
        foreach (KeyValuePair<string, byte[]> file in files) {
            Directory.CreateDirectory(Path.GetDirectoryName(file.Key)); File.WriteAllBytes(file.Key, file.Value);
        }
    }

    private static int SelfTest(string scratch)
    {
        try {
            string root = Path.GetFullPath(scratch); Directory.CreateDirectory(root);
            bool rejected = false;
            try { SafeTarget(root, "../../outside.txt"); } catch (IOException) { rejected = true; }
            if (!rejected) throw new Exception("Traversal accepted");
            string zip = Path.Combine(root, "fixture.zip");
            using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Create)) {
                using (StreamWriter writer = new StreamWriter(archive.CreateEntry("BepInExPack_EtG/BepInEx/core/BepInEx.dll").Open())) writer.Write("fixture");
                using (StreamWriter writer = new StreamWriter(archive.CreateEntry("BepInExPack_EtG/BepInEx/config/BepInEx.cfg").Open())) writer.Write("[Logging.Console]\nEnabled = true\n");
            }
            string destination = Path.Combine(root, "fake-game");
            InstallPack(zip, destination); InstallPack(zip, destination);
            if (File.ReadAllText(Path.Combine(destination, "BepInEx/config/BepInEx.cfg")).Contains("Enabled = true")) throw new Exception("Console still enabled");
            File.WriteAllText(Path.Combine(destination, "BepInEx/core/BepInEx.dll"), "preserve me");
            rejected = false;
            try { InstallPack(zip, destination); } catch (IOException) { rejected = true; }
            if (!rejected || File.ReadAllText(Path.Combine(destination, "BepInEx/core/BepInEx.dll")) != "preserve me") throw new Exception("Existing file overwritten");
            if (ResourcePlugin().Length < 1000) throw new Exception("Embedded plugin missing");
            File.WriteAllText(Path.Combine(root, "launcher-test-result.txt"), "LAUNCHER_TESTS_PASS: traversal guard, pack extraction, idempotence, console setting, collision preservation, embedded DLL\n");
            return 0;
        } catch (Exception ex) { File.WriteAllText(Path.Combine(scratch, "launcher-test-result.txt"), ex.ToString()); return 1; }
    }

    private static int PackageTest(string zip, string scratch)
    {
        try {
            Directory.CreateDirectory(scratch);
            if (Hash(File.ReadAllBytes(zip)) != PackageHash) throw new Exception("Package hash mismatch");
            InstallPack(zip, scratch); InstallPack(zip, scratch);
            if (AssemblyName.GetAssemblyName(Path.Combine(scratch, "BepInEx/core/BepInEx.dll")).Version.Major != 5) throw new Exception("Wrong BepInEx assembly");
            if (!File.Exists(Path.Combine(scratch, "winhttp.dll")) || !File.Exists(Path.Combine(scratch, "doorstop_config.ini"))) throw new Exception("Loader missing");
            if (File.ReadAllText(Path.Combine(scratch, "BepInEx/config/BepInEx.cfg")).Contains("Enabled = true\r\n\r\n## If enabled, will prevent closing")) throw new Exception("Console not disabled");
            File.WriteAllText(Path.Combine(scratch, "package-test-result.txt"), "PACKAGE_TEST_PASS: official pack hash, real BepInEx 5, loader extraction, repeated install\n");
            return 0;
        } catch (Exception ex) { File.WriteAllText(Path.Combine(scratch, "package-test-result.txt"), ex.ToString()); return 1; }
    }
}

