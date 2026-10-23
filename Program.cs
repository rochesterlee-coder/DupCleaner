using System.Globalization;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
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
            ["readError"]="Sesetengah fail tidak dapat dialihkan", ["scanError"]="Ralat imbasan", ["cancel"]="Batal", ["yes"]="Ya", ["no"]="Tidak"
        });
        AddLanguage("es", "Español", new() {
            ["tagline"]="BUSCADOR DE ARCHIVOS DUPLICADOS SIN CONEXIÓN", ["intro"]="Encuentra archivos idénticos, consulta el espacio que podrías liberar y elige cómo eliminar las copias.",
            ["folder"]="CARPETA PARA ESCANEAR", ["browse"]="Examinar…", ["scan"]="Buscar duplicados", ["scanning"]="Analizando archivos y comparando contenido…",
            ["files"]="ARCHIVOS ANALIZADOS", ["dups"]="COPIAS DUPLICADAS", ["savings"]="ESPACIO QUE PODRÍAS LIBERAR", ["results"]="RESULTADOS DEL ANÁLISIS",
            ["select"]="Seleccionar copias duplicadas (conservar la primera)", ["size"]="Tamaño", ["group"]="Grupo duplicado", ["location"]="Ubicación", ["file"]="Archivo",
            ["ready"]="Elige una carpeta. Los archivos se procesan localmente en este equipo.", ["complete"]="Análisis completado. Se encontraron {0} grupos duplicados.",
            ["none"]=" No se encontraron archivos idénticos.", ["errors"]=" No se pudieron leer {0} archivos.", ["recycle"]="Mover a la Papelera", ["permanent"]="Eliminar permanentemente",
            ["clear"]="Borrar resultados", ["selectstatus"]="Copias duplicadas seleccionadas; se conserva la primera de cada grupo.",
            ["choosefirst"]="Elige primero una carpeta válida.", ["nothing"]="Selecciona primero una o más filas.", ["confirmRecycle"]="¿Mover {0} archivos a la Papelera? Normalmente se pueden restaurar. El espacio no suele liberarse hasta vaciarla.",
            ["confirmPermanent"]="¿ELIMINAR PERMANENTEMENTE {0} archivos? Se omite la Papelera y quizá no sea posible recuperarlos.",
            ["final"]="Confirmación final: ¿eliminar permanentemente estos archivos?", ["done"]="Se eliminaron {0} archivos.", ["failed"]=" No se pudieron eliminar {0}.",
            ["cleared"]="Resultados borrados. Elige una carpeta para volver a analizar.", ["setting"]="Idioma", ["langHint"]="Elige el idioma de la interfaz. El cambio se aplica inmediatamente."
        });
        AddLanguage("fr", "Français", new() {
            ["tagline"]="RECHERCHE DE FICHIERS EN DOUBLE HORS LIGNE", ["intro"]="Trouvez les fichiers identiques, estimez l’espace récupérable et choisissez comment supprimer les copies.",
            ["folder"]="DOSSIER À ANALYSER", ["browse"]="Parcourir…", ["scan"]="Rechercher les doublons", ["scanning"]="Analyse des fichiers et comparaison du contenu…",
            ["files"]="FICHIERS ANALYSÉS", ["dups"]="COPIES EN DOUBLE", ["savings"]="ESPACE POTENTIELLEMENT LIBÉRÉ", ["results"]="RÉSULTATS DE L’ANALYSE",
            ["select"]="Sélectionner les copies en double (garder la première)", ["size"]="Taille", ["group"]="Groupe de doublons", ["location"]="Emplacement", ["file"]="Fichier",
            ["ready"]="Choisissez un dossier. Les fichiers sont traités localement sur cet ordinateur.", ["complete"]="Analyse terminée. {0} groupe(s) de doublons trouvé(s).",
            ["none"]=" Aucun fichier identique trouvé.", ["errors"]=" {0} fichier(s) illisible(s).", ["recycle"]="Déplacer vers la Corbeille",
            ["permanent"]="Supprimer définitivement", ["clear"]="Effacer les résultats", ["selectstatus"]="Copies en double sélectionnées ; le premier fichier de chaque groupe est conservé.",
            ["choosefirst"]="Choisissez d’abord un dossier valide.", ["nothing"]="Sélectionnez d’abord une ou plusieurs lignes.", ["confirmRecycle"]="Déplacer {0} fichier(s) vers la Corbeille ? Vous pourrez généralement les restaurer. L’espace n’est généralement libéré qu’après vidage de la Corbeille.",
            ["confirmPermanent"]="SUPPRIMER DÉFINITIVEMENT {0} fichier(s) ? La Corbeille sera ignorée et la récupération peut être impossible.",
            ["final"]="Confirmation finale : supprimer définitivement ces fichiers ?", ["done"]="{0} fichier(s) supprimé(s).", ["failed"]=" Échec pour {0} fichier(s).",
            ["cleared"]="Résultats effacés. Choisissez un dossier pour relancer l’analyse.", ["setting"]="Langue", ["langHint"]="Choisissez la langue de l’interface. Le changement est immédiat."
        });
        AddLanguage("de", "Deutsch", new() {
            ["tagline"]="OFFLINE-DUPLIKATDATEI-FINDER", ["intro"]="Finde identische Dateien, prüfe den möglichen Speichergewinn und wähle, wie Kopien entfernt werden.",
            ["folder"]="ZU DURCHSUCHENDER ORDNER", ["browse"]="Durchsuchen…", ["scan"]="Nach Duplikaten suchen", ["scanning"]="Dateien werden geprüft und Inhalte verglichen…",
            ["files"]="GEPRÜFTE DATEIEN", ["dups"]="DUPLIKATE", ["savings"]="MÖGLICHER SPEICHERGEWINN", ["results"]="SCAN-ERGEBNISSE",
            ["select"]="Duplikate auswählen (erste Datei behalten)", ["size"]="Größe", ["group"]="Duplikatgruppe", ["location"]="Speicherort", ["file"]="Datei",
            ["ready"]="Wähle einen Ordner. Die Dateien werden lokal auf diesem Computer verarbeitet.", ["complete"]="Scan abgeschlossen. {0} Duplikatgruppe(n) gefunden.",
            ["none"]=" Keine identischen Duplikate gefunden.", ["errors"]=" {0} Datei(en) konnten nicht gelesen werden.", ["recycle"]="In den Papierkorb verschieben",
            ["permanent"]="Ausgewählte dauerhaft löschen", ["clear"]="Ergebnisse löschen", ["selectstatus"]="Duplikate ausgewählt; die erste Datei jeder Gruppe bleibt erhalten.",
            ["choosefirst"]="Bitte wähle zuerst einen gültigen Ordner.", ["nothing"]="Wähle zuerst eine oder mehrere Zeilen aus.", ["confirmRecycle"]="{0} ausgewählte Datei(en) in den Papierkorb verschieben? Sie lassen sich normalerweise wiederherstellen. Speicherplatz wird meist erst nach dem Leeren frei.",
            ["confirmPermanent"]="{0} Datei(en) DAUERHAFT LÖSCHEN? Der Papierkorb wird umgangen. Eine Wiederherstellung ist möglicherweise nicht möglich.",
            ["final"]="Letzte Bestätigung: Diese Dateien dauerhaft löschen?", ["done"]="{0} Datei(en) entfernt.", ["failed"]=" {0} konnten nicht entfernt werden.",
            ["cleared"]="Ergebnisse gelöscht. Wähle einen Ordner für einen neuen Scan.", ["setting"]="Sprache", ["langHint"]="Wähle die Sprache der Benutzeroberfläche. Die Änderung gilt sofort."
        });
        AddLanguage("ja", "日本語", new() {
            ["tagline"]="オフライン重複ファイル検索", ["intro"]="同一ファイルを探し、削減できる容量を確認して、削除方法を選択します。",
            ["folder"]="スキャンするフォルダー", ["browse"]="参照…", ["scan"]="重複ファイルをスキャン", ["scanning"]="ファイルをスキャンして内容を比較しています…",
            ["files"]="スキャンしたファイル", ["dups"]="重複コピー", ["savings"]="削減可能な容量", ["results"]="スキャン結果",
            ["select"]="重複コピーを選択（最初のファイルを保持）", ["size"]="サイズ", ["group"]="重複グループ", ["location"]="場所", ["file"]="ファイル",
            ["ready"]="スキャンするフォルダーを選択してください。ファイルはこのPC内で処理されます。", ["complete"]="スキャン完了。重複グループが {0} 件見つかりました。",
            ["none"]=" 同一の重複ファイルは見つかりませんでした。", ["errors"]=" {0} 個のファイルを読み取れませんでした。", ["recycle"]="ごみ箱に移動",
            ["permanent"]="選択したファイルを完全に削除", ["clear"]="結果をクリア", ["selectstatus"]="重複コピーを選択しました。各グループの最初のファイルは保持されます。",
            ["choosefirst"]="有効なフォルダーを選択してください。", ["nothing"]="先にファイル行を選択してください。", ["confirmRecycle"]="選択した {0} 個のファイルをごみ箱に移動しますか？通常は復元できます。ごみ箱を空にするまで容量は通常解放されません。",
            ["confirmPermanent"]="選択した {0} 個のファイルを完全に削除しますか？ごみ箱を経由せず、復元できない可能性があります。",
            ["final"]="最終確認：これらのファイルを完全に削除しますか？", ["done"]="{0} 個のファイルを削除しました。", ["failed"]=" {0} 個のファイルを削除できませんでした。",
            ["cleared"]="結果をクリアしました。再スキャンするフォルダーを選択してください。", ["setting"]="言語", ["langHint"]="画面の言語を選択します。変更はすぐに反映されます。"
        });
        AddLanguage("ar", "العربية", new() {
            ["tagline"]="البحث عن الملفات المكررة دون اتصال", ["intro"]="اعثر على الملفات المتطابقة، واعرف المساحة التي يمكن توفيرها، ثم اختر طريقة إزالة النسخ.",
            ["folder"]="المجلد المراد فحصه", ["browse"]="استعراض…", ["scan"]="فحص الملفات المكررة", ["scanning"]="جارٍ فحص الملفات ومقارنة محتوياتها…",
            ["files"]="الملفات التي تم فحصها", ["dups"]="النسخ المكررة", ["savings"]="المساحة المحتمل توفيرها", ["results"]="نتائج الفحص",
            ["select"]="تحديد النسخ المكررة (الاحتفاظ بالأولى)", ["size"]="الحجم", ["group"]="مجموعة التكرار", ["location"]="الموقع", ["file"]="الملف",
            ["ready"]="اختر مجلداً للفحص. تتم معالجة الملفات محلياً على هذا الكمبيوتر.", ["complete"]="اكتمل الفحص. تم العثور على {0} مجموعة مكررة.",
            ["none"]=" لم يتم العثور على ملفات متطابقة.", ["errors"]=" تعذرت قراءة {0} ملفاً.", ["recycle"]="نقل المحدد إلى سلة المحذوفات",
            ["permanent"]="حذف المحدد نهائياً", ["clear"]="مسح النتائج", ["selectstatus"]="تم تحديد النسخ المكررة مع الاحتفاظ بأول ملف في كل مجموعة.",
            ["choosefirst"]="يرجى اختيار مجلد صالح أولاً.", ["nothing"]="حدد صفاً واحداً أو أكثر أولاً.", ["confirmRecycle"]="هل تريد نقل {0} ملفاً إلى سلة المحذوفات؟ يمكن عادةً استعادتها. لا تُسترد المساحة عادةً حتى إفراغ السلة.",
            ["confirmPermanent"]="هل تريد حذف {0} ملفاً نهائياً؟ سيتم تجاوز سلة المحذوفات وقد يتعذر الاسترداد.",
            ["final"]="تأكيد أخير: هل تريد حذف هذه الملفات نهائياً الآن؟", ["done"]="تمت إزالة {0} ملف.", ["failed"]=" تعذرت إزالة {0} ملف.",
            ["cleared"]="تم مسح النتائج. اختر مجلداً لإعادة الفحص.", ["setting"]="اللغة", ["langHint"]="اختر لغة الواجهة. يُطبّق التغيير فوراً."
        });
        AddLanguage("hi", "हिन्दी", new() {
            ["tagline"]="ऑफ़लाइन डुप्लिकेट फ़ाइल खोजक", ["intro"]="एक जैसी फ़ाइलें खोजें, संभावित रूप से बचने वाली जगह देखें और कॉपी हटाने का तरीका चुनें।",
            ["folder"]="स्कैन करने वाला फ़ोल्डर", ["browse"]="ब्राउज़ करें…", ["scan"]="डुप्लिकेट स्कैन करें", ["scanning"]="फ़ाइलें स्कैन हो रही हैं और सामग्री की तुलना हो रही है…",
            ["files"]="स्कैन की गई फ़ाइलें", ["dups"]="डुप्लिकेट कॉपी", ["savings"]="संभावित खाली जगह", ["results"]="स्कैन परिणाम",
            ["select"]="डुप्लिकेट कॉपी चुनें (पहली रखें)", ["size"]="आकार", ["group"]="डुप्लिकेट समूह", ["location"]="स्थान", ["file"]="फ़ाइल",
            ["ready"]="स्कैन करने के लिए फ़ोल्डर चुनें। फ़ाइलें इसी कंप्यूटर पर प्रोसेस होती हैं।", ["complete"]="स्कैन पूरा। {0} डुप्लिकेट समूह मिले।",
            ["none"]=" कोई समान डुप्लिकेट फ़ाइल नहीं मिली।", ["errors"]=" {0} फ़ाइलें पढ़ी नहीं जा सकीं।", ["recycle"]="चुनी हुई फ़ाइलें रीसायकल बिन में भेजें",
            ["permanent"]="चुनी हुई फ़ाइलें स्थायी रूप से हटाएँ", ["clear"]="परिणाम साफ़ करें", ["selectstatus"]="डुप्लिकेट कॉपी चुनी गईं; हर समूह की पहली फ़ाइल रखी जाएगी।",
            ["choosefirst"]="पहले एक मान्य फ़ोल्डर चुनें।", ["nothing"]="पहले एक या अधिक फ़ाइल पंक्तियाँ चुनें।", ["confirmRecycle"]="चुनी गई {0} फ़ाइलें रीसायकल बिन में भेजें? आमतौर पर उन्हें वापस लाया जा सकता है। बिन खाली होने तक जगह आमतौर पर खाली नहीं होती।",
            ["confirmPermanent"]="चुनी गई {0} फ़ाइलें हमेशा के लिए हटाएँ? रीसायकल बिन को छोड़ दिया जाएगा और फ़ाइलें वापस पाना संभव न हो।",
            ["final"]="अंतिम पुष्टि: क्या इन फ़ाइलों को स्थायी रूप से हटाएँ?", ["done"]="{0} फ़ाइलें हटाई गईं।", ["failed"]=" {0} फ़ाइलें नहीं हट सकीं।",
            ["cleared"]="परिणाम साफ़ किए गए। फिर से स्कैन करने के लिए फ़ोल्डर चुनें।", ["setting"]="भाषा", ["langHint"]="इंटरफ़ेस की भाषा चुनें। बदलाव तुरंत लागू होगा।"
        });
        AddLanguage("pt-BR", "Português (Brasil)", new() {
            ["tagline"]="LOCALIZADOR DE ARQUIVOS DUPLICADOS OFFLINE", ["intro"]="Encontre arquivos idênticos, confira o espaço que pode liberar e escolha como remover cópias.",
            ["folder"]="PASTA PARA ANALISAR", ["browse"]="Procurar…", ["scan"]="Buscar duplicados", ["scanning"]="Analisando arquivos e comparando conteúdo…",
            ["files"]="ARQUIVOS ANALISADOS", ["dups"]="CÓPIAS DUPLICADAS", ["savings"]="ESPAÇO POTENCIALMENTE LIBERADO", ["results"]="RESULTADOS DA ANÁLISE",
            ["select"]="Selecionar cópias duplicadas (manter a primeira)", ["size"]="Tamanho", ["group"]="Grupo duplicado", ["location"]="Local", ["file"]="Arquivo",
            ["ready"]="Escolha uma pasta. Os arquivos são processados localmente neste computador.", ["complete"]="Análise concluída. Encontrados {0} grupos duplicados.",
            ["none"]=" Nenhum arquivo idêntico encontrado.", ["errors"]=" {0} arquivo(s) não puderam ser lidos.", ["recycle"]="Mover para a Lixeira",
            ["permanent"]="Excluir permanentemente", ["clear"]="Limpar resultados", ["selectstatus"]="Cópias duplicadas selecionadas; o primeiro arquivo de cada grupo será mantido.",
            ["choosefirst"]="Escolha primeiro uma pasta válida.", ["nothing"]="Selecione uma ou mais linhas primeiro.", ["confirmRecycle"]="Mover {0} arquivo(s) para a Lixeira? Normalmente é possível restaurá-los. O espaço geralmente só é liberado ao esvaziar a Lixeira.",
            ["confirmPermanent"]="EXCLUIR PERMANENTEMENTE {0} arquivo(s)? Isso ignora a Lixeira e a recuperação pode ser impossível.",
            ["final"]="Confirmação final: excluir estes arquivos permanentemente?", ["done"]="{0} arquivo(s) removido(s).", ["failed"]=" {0} não puderam ser removidos.",
            ["cleared"]="Resultados limpos. Escolha uma pasta para analisar novamente.", ["setting"]="Idioma", ["langHint"]="Escolha o idioma da interface. A mudança é imediata."
        });
    }

    private void AddLanguage(string code, string name, Dictionary<string, string> values)
    {
        values["__name"] = name;
        translations[code] = values;
    }
    private string T(string key, params object[] args)
    {
        string value = translations.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var v)
            ? v : translations["en"].GetValueOrDefault(key, key);
        try { return string.Format(CultureInfo.InvariantCulture, value, args); } catch { return value; }
    }

    private Label LabelAt(string key, int x, int y, int size = 9, bool bold = false, Color? color = null)
        => new() { Text = T(key), Location = new Point(x, y), AutoSize = true,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color ?? Color.FromArgb(51, 65, 85) };

    private Button ButtonFor(string key, bool primary = false)
    {
        var b = new Button { Text = T(key), AutoSize = true, Padding = new Padding(10, 7, 10, 7),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Color.FromArgb(37, 99, 235) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(51, 65, 85), Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(4) };
        b.FlatAppearance.BorderColor = primary ? Color.FromArgb(37, 99, 235) : Color.FromArgb(218, 226, 237);
        return b;
    }

    private readonly Panel header = new(), folderPanel = new(), statsPanel = new(), tablePanel = new(), footer = new(), celebrationPanel = new();
    private readonly Label celebrationLabel = new();
    private readonly Panel logoPanel = new();
    private readonly Label titleLabel = new(), taglineLabel = new(), introLabel = new(), folderLabel = new(), resultsLabel = new(), languageLabel = new(), languageHint = new();
    private readonly Label[] statTitles = new Label[3];

    private void BuildUi()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(980, 700);
        Size = new Size(1240, 820);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(18), BackColor = BackColor };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
        Controls.Add(root);

        header.Dock = DockStyle.Fill; header.BackColor = Color.White; header.Padding = new Padding(14, 10, 14, 8);
        logoPanel.Location = new Point(12, 18); logoPanel.Size = new Size(72, 72); logoPanel.BackColor = Color.Transparent;
        logoPanel.Paint += (_, e) => DrawLogo(e.Graphics, logoPanel.ClientRectangle);
        titleLabel.Text = "DupClean"; titleLabel.Location = new Point(94, 12); titleLabel.AutoSize = true; titleLabel.Font = new Font("Segoe UI", 27, FontStyle.Bold); titleLabel.ForeColor = Color.FromArgb(23, 37, 84);
        taglineLabel.Location = new Point(96, 58); taglineLabel.AutoSize = true; taglineLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold); taglineLabel.ForeColor = Color.FromArgb(37, 99, 235);
        introLabel.Location = new Point(96, 82); introLabel.AutoSize = false; introLabel.Size = new Size(600, 28); introLabel.Font = new Font("Segoe UI", 9.5F); introLabel.ForeColor = Color.FromArgb(100, 116, 139); introLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        languageLabel.AutoSize = true; languageLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        language.DropDownStyle = ComboBoxStyle.DropDownList; language.Width = 220; language.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        language.Items.AddRange(translations.Select(kv => new LanguageItem(kv.Key, kv.Value["__name"])).ToArray());
        language.SelectedIndexChanged += (_, _) => { if (language.SelectedItem is LanguageItem item) { lang = item.Code; ApplyLanguage(); } };
        languageHint.AutoSize = false; languageHint.Size = new Size(330, 34); languageHint.Font = new Font("Segoe UI", 8); languageHint.ForeColor = Color.FromArgb(100, 116, 139); languageHint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        header.Controls.AddRange(new Control[] { logoPanel, titleLabel, taglineLabel, introLabel, languageLabel, language, languageHint });
        header.Resize += (_, _) =>
        {
            int right = header.ClientSize.Width - 18;
            language.Left = Math.Max(620, right - language.Width); language.Top = 12;
            languageLabel.Left = Math.Max(600, language.Left - languageLabel.Width - 8); languageLabel.Top = 16;
            languageHint.Left = languageLabel.Left; languageHint.Top = 43; languageHint.Width = Math.Max(180, right - languageHint.Left);
            introLabel.Width = Math.Max(280, languageLabel.Left - introLabel.Left - 18);
        };
        root.Controls.Add(header, 0, 0);

        folderPanel.Dock = DockStyle.Fill;
        folderLabel.Location = new Point(2, 0); folderLabel.AutoSize = true; folderLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        folderBox.Location = new Point(0, 25); folderBox.Height = 30; folderBox.Font = new Font("Segoe UI", 10.5F); folderBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        browse.Height = scan.Height = 34; browse.Click += (_, _) => BrowseFolder(); scan.Click += async (_, _) => await ScanAsync();
        folderPanel.Controls.AddRange(new Control[] { folderLabel, folderBox, browse, scan });
        folderPanel.Resize += (_, _) =>
        {
            scan.Width = 132; browse.Width = 104; scan.Left = folderPanel.ClientSize.Width - scan.Width; browse.Left = scan.Left - browse.Width - 8;
            folderBox.Width = Math.Max(120, browse.Left - 10); browse.Top = scan.Top = 22;
        };
        root.Controls.Add(folderPanel, 0, 1);

        var center = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0, 4, 0, 0) };
        center.RowStyles.Add(new RowStyle(SizeType.Absolute, 86)); center.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        statsPanel.Dock = DockStyle.Fill;
        var stats = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(0, 0, 0, 8) };
        for (int i = 0; i < 3; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        filesValue.Font = duplicatesValue.Font = savingsValue.Font = new Font("Segoe UI", 19, FontStyle.Bold);
        filesValue.ForeColor = Color.FromArgb(23, 37, 84); duplicatesValue.ForeColor = Color.FromArgb(220, 38, 38); savingsValue.ForeColor = Color.FromArgb(5, 150, 105);
        var values = new[] { filesValue, duplicatesValue, savingsValue };
        string[] statKeys = { "files", "dups", "savings" };
        Color[] cardColors = { Color.FromArgb(239, 246, 255), Color.FromArgb(255, 241, 242), Color.FromArgb(236, 253, 245) };
        for (int i = 0; i < 3; i++)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = cardColors[i], Margin = new Padding(0, 0, 10, 0), Padding = new Padding(12, 8, 8, 4) };
            statTitles[i] = new Label { Location = new Point(12, 7), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(71, 85, 105) };
            values[i].Location = new Point(12, 30); values[i].AutoSize = true; values[i].Text = "—";
            card.Controls.Add(statTitles[i]); card.Controls.Add(values[i]); stats.Controls.Add(card, i, 0);
        }
        statsPanel.Controls.Add(stats); center.Controls.Add(statsPanel, 0, 0);

        tablePanel.Dock = DockStyle.Fill; tablePanel.BackColor = Color.White;
        var tableHeader = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.FromArgb(248, 250, 252) };
        resultsLabel.Location = new Point(14, 12); resultsLabel.AutoSize = true; resultsLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        selectDuplicates.Anchor = AnchorStyles.Top | AnchorStyles.Right; selectDuplicates.Click += (_, _) => SelectDuplicateCopies();
        tableHeader.Controls.AddRange(new Control[] { resultsLabel, selectDuplicates });
        tableHeader.Resize += (_, _) => selectDuplicates.Left = Math.Max(160, tableHeader.ClientSize.Width - selectDuplicates.Width - 10);
        grid.Dock = DockStyle.Fill; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.ReadOnly = true; grid.MultiSelect = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.RowHeadersVisible = false; grid.AutoGenerateColumns = false;
        grid.BackgroundColor = Color.White; grid.BorderStyle = BorderStyle.None; grid.EnableHeadersVisualStyles = false; grid.GridColor = Color.FromArgb(226, 232, 240);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249); grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59); grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold); grid.ColumnHeadersHeight = 34;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F); grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254); grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59); grid.RowTemplate.Height = 29;
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "file", HeaderText = "File", Width = 240 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "size", HeaderText = "Size", Width = 110 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "group", HeaderText = "Duplicate group", Width = 145 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "path", HeaderText = "Location", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 180 });
        tablePanel.Controls.Add(grid); tablePanel.Controls.Add(tableHeader);
        center.Controls.Add(tablePanel, 0, 1); root.Controls.Add(center, 0, 2);

        footer.Dock = DockStyle.Fill;
        var footLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        footLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8)); footLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27)); footLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43)); footLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        progress.Dock = DockStyle.Fill; progress.Margin = new Padding(0, 0, 0, 3);
        status.Dock = DockStyle.Fill; status.TextAlign = ContentAlignment.MiddleLeft; status.AutoEllipsis = true;
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 2, 0, 0) };
        recycle.Click += (_, _) => RemoveSelected(false); permanent.Click += (_, _) => RemoveSelected(true); clear.Click += (_, _) => ClearResults();
        recycle.Height = permanent.Height = clear.Height = 34;
        actions.Controls.AddRange(new Control[] { recycle, permanent, clear });
        celebrationPanel.Dock = DockStyle.Fill; celebrationPanel.BackColor = Color.FromArgb(220, 252, 231); celebrationPanel.Visible = false; celebrationPanel.Padding = new Padding(12, 4, 12, 4);
        celebrationLabel.Dock = DockStyle.Fill; celebrationLabel.TextAlign = ContentAlignment.MiddleLeft; celebrationLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold); celebrationLabel.ForeColor = Color.FromArgb(22, 101, 52);
        celebrationPanel.Controls.Add(celebrationLabel);
        footLayout.Controls.Add(progress, 0, 0); footLayout.Controls.Add(status, 0, 1); footLayout.Controls.Add(actions, 0, 2); footLayout.Controls.Add(celebrationPanel, 0, 3);
        footer.Controls.Add(footLayout); root.Controls.Add(footer, 0, 3);
        language.SelectedItem = language.Items.Cast<LanguageItem>().First(x => x.Code == "en");
    }

    private static void DrawLogo(Graphics g, Rectangle bounds)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var blue = new SolidBrush(Color.FromArgb(37, 99, 235));
        using var violet = new SolidBrush(Color.FromArgb(124, 58, 237));
        using var pale = new SolidBrush(Color.FromArgb(219, 234, 254));
        g.FillRectangle(pale, 6, 20, 44, 43);
        g.FillRectangle(blue, 12, 14, 42, 43);
        using var whitePen = new Pen(Color.White, 3);
        g.DrawLine(whitePen, 23, 28, 43, 28); g.DrawLine(whitePen, 23, 36, 39, 36); g.DrawLine(whitePen, 23, 44, 35, 44);
        using var lensPen = new Pen(violet, 7); g.DrawEllipse(lensPen, 30, 3, 30, 30); g.DrawLine(lensPen, 55, 28, 68, 42);
        using var innerPen = new Pen(Color.FromArgb(96, 165, 250), 3); g.DrawEllipse(innerPen, 36, 9, 18, 18);
    }

    private sealed record LanguageItem(string Code, string Name) { public override string ToString() => Name; }

    private void ApplyLanguage()
    {
        Text = "DupClean";
        taglineLabel.Text = T("tagline"); introLabel.Text = T("intro"); folderLabel.Text = T("folder");
        browse.Text = T("browse"); scan.Text = T("scan"); languageLabel.Text = T("setting") + ":";
        languageHint.Text = T("langHint"); resultsLabel.Text = T("results");
        selectDuplicates.Text = T("select"); recycle.Text = T("recycle"); permanent.Text = T("permanent"); clear.Text = T("clear");
        status.Text = T("ready");
        string[] keys = { "files", "dups", "savings" };
        for (int i = 0; i < 3; i++) statTitles[i].Text = T(keys[i]);
        grid.Columns["file"]!.HeaderText = T("file"); grid.Columns["size"]!.HeaderText = T("size");
        grid.Columns["group"]!.HeaderText = T("group"); grid.Columns["path"]!.HeaderText = T("location");
        RightToLeft = lang == "ar" ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = lang == "ar";
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = T("folder") };
        if (dialog.ShowDialog(this) == DialogResult.OK) folderBox.Text = dialog.SelectedPath;
    }

    private async Task ScanAsync()
    {
        if (scanning) return;
        string folder = folderBox.Text.Trim();
        if (!Directory.Exists(folder)) { MessageBox.Show(this, T("choosefirst"), "DupClean", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        scanning = true; scan.Enabled = false; progress.Style = ProgressBarStyle.Marquee; status.Text = T("scanning");
        grid.Rows.Clear(); found.Clear(); celebrationPanel.Visible = false; filesValue.Text = duplicatesValue.Text = savingsValue.Text = "—";
        try
        {
            var result = await Task.Run(() => FindDuplicates(folder));
            filesValue.Text = result.Count.ToString("N0");
            long potential = 0; int duplicateCount = 0;
            int groupNumber = 0;
            foreach (var group in result.Groups)
            {
                groupNumber++;
                for (int i = 0; i < group.Count; i++)
                {
                    var item = new FileResult { Path = group[i], Size = SafeSize(group[i]), Group = groupNumber, Keep = i == 0 };
                    found.Add(item);
                    grid.Rows.Add(System.IO.Path.GetFileName(item.Path), FormatBytes(item.Size), $"Group {item.Group}", item.Path);
                    if (i > 0) { potential += item.Size; duplicateCount++; }
                }
            }
            duplicatesValue.Text = duplicateCount.ToString("N0"); savingsValue.Text = FormatBytes(potential);
            status.Text = T("complete", result.Groups.Count) + (result.Errors > 0 ? T("errors", result.Errors) : "") + (result.Groups.Count == 0 ? T("none") : "");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, T("scanError"), MessageBoxButtons.OK, MessageBoxIcon.Error); status.Text = T("scanError"); }
        finally { progress.Style = ProgressBarStyle.Blocks; progress.Value = 0; scan.Enabled = true; scanning = false; }
    }

    private sealed record ScanResult(int Count, List<List<string>> Groups, int Errors);

    private static ScanResult FindDuplicates(string root)
    {
        int count = 0, errors = 0; var sizes = new Dictionary<long, List<string>>();
        foreach (string path in EnumerateFiles(root, ref errors))
        {
            try { long size = new FileInfo(path).Length; count++; if (!sizes.TryGetValue(size, out var list)) sizes[size] = list = new(); list.Add(path); }
            catch { errors++; }
        }
        var hashes = new Dictionary<string, List<string>>();
        foreach (var pair in sizes.Where(p => p.Value.Count > 1))
        foreach (string path in pair.Value)
        {
            try
            {
                using var stream = File.OpenRead(path);
                string hash = Convert.ToHexString(SHA256.HashData(stream));
                string key = pair.Key + ":" + hash;
                if (!hashes.TryGetValue(key, out var list)) hashes[key] = list = new();
                list.Add(path);
            }
            catch { errors++; }
        }
        return new ScanResult(count, hashes.Values.Where(g => g.Count > 1)
            .Select(g => g.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList()).ToList(), errors);
    }

    private static List<string> EnumerateFiles(string root, ref int errors)
    {
        var list = new List<string>(); var stack = new Stack<string>(); stack.Push(root);
        while (stack.Count > 0)
        {
            string dir = stack.Pop(); string[] dirs = Array.Empty<string>(), files = Array.Empty<string>();
            try { dirs = Directory.GetDirectories(dir); } catch { errors++; }
            try { files = Directory.GetFiles(dir); } catch { errors++; }
            foreach (var sub in dirs)
            {
                string name = System.IO.Path.GetFileName(sub);
                if (name.Equals("$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase) || name.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase)) continue;
                try { if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) == 0) stack.Push(sub); } catch { }
            }
            foreach (var file in files)
                try { if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0) list.Add(file); } catch { errors++; }
        }
        return list;
    }

    private void SelectDuplicateCopies()
    {
        grid.ClearSelection(); var seen = new HashSet<int>();
        for (int i = 0; i < found.Count && i < grid.Rows.Count; i++)
        {
            if (seen.Add(found[i].Group)) continue;
            grid.Rows[i].Selected = true;
        }
        status.Text = T("selectstatus");
    }

    // Keep Windows from entering system sleep while file cleanup is running.
    // ES_CONTINUOUS restores normal power policy when cleanup finishes.
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;

    private void RemoveSelected(bool permanently)
    {
        var selected = grid.SelectedRows.Cast<DataGridViewRow>().Where(r => r.Index >= 0 && r.Index < found.Count).Select(r => r.Index).OrderByDescending(i => i).ToList();
        if (selected.Count == 0) { MessageBox.Show(this, T("nothing"), "DupClean", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        string message = permanently ? T("confirmPermanent", selected.Count) : T("confirmRecycle", selected.Count);
        if (MessageBox.Show(this, message, "DupClean", MessageBoxButtons.YesNo, permanently ? MessageBoxIcon.Warning : MessageBoxIcon.Question) != DialogResult.Yes) return;
        if (permanently && MessageBox.Show(this, T("final"), "DupClean", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;
        int done = 0; var failed = new List<string>();
        // Ask Windows to keep the system awake during the delete/move operation.
        SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
        try
        {
            foreach (int index in selected)
            {
                var item = found[index];
                try
                {
                    if (permanently) File.Delete(item.Path);
                    else FileSystem.DeleteFile(item.Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    grid.Rows.RemoveAt(index); found.RemoveAt(index); done++;
                }
                catch (Exception ex) { failed.Add($"{item.Path}: {ex.Message}"); }
            }
        }
        finally
        {
            // Restore the user's normal Windows power settings, even if cleanup errors.
            SetThreadExecutionState(ES_CONTINUOUS);
        }
        RefreshSavings();
        status.Text = T("done", done) + (failed.Count > 0 ? T("failed", failed.Count) : "");
        if (done > 0 && failed.Count == 0)
        {
            celebrationLabel.Text = $"🏆  Cleanup complete!  Removed {done:N0} duplicate file(s).  Great job keeping your files tidy!  🎉";
            celebrationPanel.Visible = true;
        }
        if (failed.Count > 0) MessageBox.Show(this, string.Join("\n", failed.Take(8)), T("readError"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void RefreshSavings()
    {
        long bytes = 0; int count = 0;
        foreach (var g in found.GroupBy(f => f.Group))
        {
            int copies = Math.Max(0, g.Count() - 1);
            bytes += g.First().Size * copies; count += copies;
        }
        duplicatesValue.Text = count.ToString("N0"); savingsValue.Text = FormatBytes(bytes);
    }

    private void ClearResults()
    {
        if (scanning) return;
        grid.Rows.Clear(); found.Clear(); celebrationPanel.Visible = false; filesValue.Text = duplicatesValue.Text = savingsValue.Text = "—"; status.Text = T("cleared");
    }

    private static long SafeSize(string path) { try { return new FileInfo(path).Length; } catch { return 0; } }
    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" }; double value = bytes; int i = 0;
        while (value >= 1024 && i < units.Length - 1) { value /= 1024; i++; }
        return i == 0 ? $"{bytes:N0} B" : $"{value:N2} {units[i]}";
    }
}
