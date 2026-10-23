using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;

namespace DupClean;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class FileResult
{
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public int Group { get; set; }
    public bool Keep { get; set; }
}

internal sealed class MainForm : Form
{
    private readonly ComboBox language = new();
    private readonly TextBox folderBox = new();
    private readonly Button browse = new(), scan = new(), selectDuplicates = new(),
        recycle = new(), permanent = new(), clear = new();
    private readonly DataGridView grid = new();
    private readonly Label filesValue = new(), duplicatesValue = new(), savingsValue = new(), status = new();
    private readonly ProgressBar progress = new();
    private readonly List<FileResult> found = new();
    private readonly Dictionary<string, Dictionary<string, string>> translations = new();
    private string lang = "en";
    private bool scanning;

    public MainForm()
    {
        BuildTranslations();
        Text = "DupClean";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 620);
        Size = new Size(1120, 760);
        BackColor = Color.FromArgb(246, 248, 252);
        Font = new Font("Segoe UI", 9F);
        BuildUi();
        ApplyLanguage();
    }

    private void BuildTranslations()
    {
        // Built-in translations are available offline. "Other languages" can use a
        // custom JSON translation file (format documented in README).
        AddLanguage("en", "English", new() {
            ["tagline"]="OFFLINE DUPLICATE FILE FINDER", ["intro"]="Find identical files, review potential storage savings, and choose how to remove copies.",
            ["folder"]="FOLDER TO SCAN", ["browse"]="Browse…", ["scan"]="Scan for duplicates", ["scanning"]="Scanning files and comparing contents…",
            ["files"]="FILES SCANNED", ["dups"]="DUPLICATE COPIES", ["savings"]="POTENTIAL SPACE SAVED", ["results"]="SCAN RESULTS",
            ["select"]="Select duplicate copies (keep first)", ["size"]="Size", ["group"]="Duplicate group", ["location"]="Location", ["file"]="File",
            ["ready"]="Choose a folder to scan. Files are processed locally on this computer.", ["complete"]="Scan complete. Found {0} duplicate group(s).",
            ["none"]=" No identical duplicates found.", ["errors"]=" {0} file(s) could not be read.", ["recycle"]="Move selected to Recycle Bin",
            ["permanent"]="Permanently delete selected", ["clear"]="Clear results", ["selectstatus"]="Selected duplicate copies, keeping the first file in each group.",
            ["choosefirst"]="Choose a valid folder first.", ["nothing"]="Select one or more file rows first.", ["confirmRecycle"]="Move {0} selected file(s) to the Recycle Bin? They can usually be restored. Space is generally not freed until the bin is emptied.",
            ["confirmPermanent"]="PERMANENTLY delete {0} selected file(s)? This bypasses the Recycle Bin. Recovery may not be possible.",
            ["final"]="Final confirmation: permanently delete these files now?", ["done"]="Removed {0} file(s).", ["failed"]=" {0} could not be removed.",
            ["cleared"]="Results cleared. Choose a folder to scan again.", ["setting"]="Language", ["langHint"]="Choose the interface language. This setting applies immediately.",
            ["readError"]="Some files could not be removed", ["scanError"]="Scan error", ["cancel"]="Cancel", ["yes"]="Yes", ["no"]="No"
        });
        AddLanguage("zh-CN", "简体中文", new() {
            ["tagline"]="离线重复文件查找器", ["intro"]="查找内容相同的文件，查看预计可释放的空间，然后选择删除方式。",
            ["folder"]="扫描文件夹", ["browse"]="浏览…", ["scan"]="扫描重复文件", ["scanning"]="正在扫描文件并比较内容…",
            ["files"]="已扫描文件", ["dups"]="重复副本", ["savings"]="预计可释放空间", ["results"]="扫描结果",
            ["select"]="选择重复副本（保留第一份）", ["size"]="大小", ["group"]="重复组", ["location"]="位置", ["file"]="文件",
            ["ready"]="请选择要扫描的文件夹。所有处理都在本机进行。", ["complete"]="扫描完成。找到 {0} 组重复文件。",
            ["none"]=" 未发现内容相同的重复文件。", ["errors"]=" 有 {0} 个文件无法读取。", ["recycle"]="移至回收站",
            ["permanent"]="永久删除所选文件", ["clear"]="清除结果", ["selectstatus"]="已选择重复副本，并保留每组第一份文件。",
            ["choosefirst"]="请先选择有效的文件夹。", ["nothing"]="请先在列表中选择一个或多个文件。", ["confirmRecycle"]="确定将选中的 {0} 个文件移至回收站吗？通常可以还原。清空回收站前一般不会释放磁盘空间。",
            ["confirmPermanent"]="确定永久删除选中的 {0} 个文件吗？此操作会绕过回收站，文件可能无法恢复。",
            ["final"]="最后确认：现在永久删除这些文件吗？", ["done"]="已移除 {0} 个文件。", ["failed"]=" 有 {0} 个文件无法移除。",
            ["cleared"]="结果已清除。请重新选择文件夹进行扫描。", ["setting"]="语言", ["langHint"]="选择界面语言。更改会立即生效。",
            ["readError"]="部分文件无法移除", ["scanError"]="扫描错误", ["cancel"]="取消", ["yes"]="是", ["no"]="否"
        });
        AddLanguage("ms", "Bahasa Melayu", new() {
            ["tagline"]="PENCARI FAIL PENDUA LUAR TALIAN", ["intro"]="Cari fail yang sama, lihat anggaran ruang storan yang boleh dijimatkan, dan pilih cara untuk membuang salinan.",
            ["folder"]="FOLDER UNTUK DIIMBAS", ["browse"]="Semak imbas…", ["scan"]="Imbas fail pendua", ["scanning"]="Sedang mengimbas fail dan membandingkan kandungan…",
            ["files"]="FAIL DIIMBAS", ["dups"]="SALINAN PENDUA", ["savings"]="ANGGARAN RUANG DIJIMATKAN", ["results"]="HASIL IMBASAN",
            ["select"]="Pilih salinan pendua (kekalkan yang pertama)", ["size"]="Saiz", ["group"]="Kumpulan pendua", ["location"]="Lokasi", ["file"]="Fail",
            ["ready"]="Pilih folder untuk diimbas. Fail diproses secara setempat pada komputer ini.", ["complete"]="Imbasan selesai. Menemui {0} kumpulan pendua.",
            ["none"]=" Tiada fail pendua yang sama ditemui.", ["errors"]=" {0} fail tidak dapat dibaca.", ["recycle"]="Pindahkan pilihan ke Tong Kitar Semula",
            ["permanent"]="Padam pilihan secara kekal", ["clear"]="Kosongkan hasil", ["selectstatus"]="Salinan pendua dipilih; fail pertama setiap kumpulan dikekalkan.",
            ["choosefirst"]="Sila pilih folder yang sah dahulu.", ["nothing"]="Pilih satu atau lebih baris fail dahulu.", ["confirmRecycle"]="Pindahkan {0} fail yang dipilih ke Tong Kitar Semula? Fail biasanya boleh dipulihkan. Ruang biasanya tidak dibebaskan sehingga tong dikosongkan.",
            ["confirmPermanent"]="PADAM {0} fail yang dipilih secara KEKAL? Tindakan ini memintas Tong Kitar Semula. Pemulihan mungkin mustahil.",
            ["final"]="Pengesahan terakhir: padam fail ini secara kekal sekarang?", ["done"]="{0} fail dialihkan.", ["failed"]=" {0} gagal dialihkan.",
            ["cleared"]="Hasil dikosongkan. Pilih folder untuk mengimbas semula.", ["setting"]="Bahasa", ["langHint"]="Pilih bahasa antara muka. Perubahan digunakan serta-merta.",
