using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using SFB;
using UnityEngine;

public class Files : MonoBehaviour {
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera screenshotCamera;

    private BlockDatabase blockData;
    private BlockPlacer placer;

    private string title;
    private float buildHeight;
    private int boardSize;
    private const int limit = 14;

    private void Start() {
        blockData = FindObjectOfType<BlockDatabase>();
        placer = FindObjectOfType<BlockPlacer>();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // Uloží stavbu jako JSON (kvůli jednoduché práci s ním) do souboru.
    public bool SaveToFile(Project project) {
        string path = StandaloneFileBrowser.SaveFilePanel("Where do you want to save your project", "", "build.json", "json");
        if (path.Equals("")) return false;
            
        string json = JsonUtility.ToJson(project);
        using StreamWriter sw = new StreamWriter(path);
        sw.Write(json);
        return true;
    }

    // Načte informace o blocích dané stavby ze souboru do datové struktury, kterou využijeme na položení bloků.
    public Project LoadFromFile() {
        string json;
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Choose a file to load a project from", "", "json", false);
        if (paths.Length == 0 || !File.Exists(paths[0])) return null;
        string path = paths[0];

        using (StreamReader sr = new StreamReader(path)) {
            json = sr.ReadToEnd();
        }
        return JsonUtility.FromJson<Project>(json);
    }

    // Funkce vytvoří PDF dokument, obsahující instrukce pro sestavení naši stavby, kdybychom se ji rozhodli
    // postavit například s reálnými bloky. Využíjeme k tomu knihovnu iTextSharp.
    public void GenerateInstructions(List<BlockHandler> blocks, string projectName, int size) {
        string path = StandaloneFileBrowser.SaveFilePanel("Where do you want to generate instructions", "",
            "instructions.pdf", "pdf");
        if (path.Equals("")) return;
        
        FileStream fs = new FileStream(path, FileMode.Create);
        Document document = new Document(PageSize.A4.Rotate(), 30, 30, 25, 25);
        PdfWriter writer = PdfWriter.GetInstance(document, fs);

        title = projectName;
        boardSize = size;
        buildHeight = Mathf.RoundToInt(blocks.Max(block => block.GetHeight() / 1.21f)) + 1;
        if (buildHeight > limit) {
            buildHeight = limit;
        }
        SetMetaData(document);
        GenerateDocument(blocks,document,writer);

        document.Close();
        writer.Close();
        fs.Close();
        Process.Start(path);
    }

    private void SetMetaData(Document document) {
        document.AddCreator("Hubelino Editor");
        document.AddSubject("Instruction on how to build a custom Hubelino Marble run track.");
        document.AddTitle("My Hubelino Marble run track - Instructions");
        document.Open();
    }

    private void GenerateDocument(List<BlockHandler> blocks, Document document, PdfWriter writer) {
        BaseFont font = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
        PdfContentByte cb = writer.DirectContent;

        GenerateFrontPage(font, cb);
        document.NewPage();
        GenerateRequirements(blocks, font, cb, false);
        GenerateLayers(document, font, cb);
    }

    // Funkce vygenreuje úvodní stránku. Zobrazí 2D pohled na stavebnici a nějaký text.
    private void GenerateFrontPage(BaseFont font, PdfContentByte cb) {
        cb.BeginText();
        Image img = Image.GetInstance(TakeScreenshot());
        //img.SetAbsolutePosition(-225,-65);
        img.ScalePercent(75);
        img.SetAbsolutePosition((PageSize.A4.Height - img.ScaledWidth) / 2, (PageSize.A4.Width - img.ScaledHeight) / 2);
        cb.AddImage(img);
        
        cb.SetColorFill(new BaseColor(136, 195, 82));
        cb.SetFontAndSize(font, 56);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT,"HUBELINO",50,500,0);
        cb.SetColorFill(BaseColor.BLACK);
        cb.SetFontAndSize(font, 42);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT,"EDITOR",50,460,0);

        cb.SetFontAndSize(font, 24);
        cb.ShowTextAligned(PdfContentByte.ALIGN_RIGHT,title,790,70,0);
        cb.ShowTextAligned(PdfContentByte.ALIGN_RIGHT,"Instructions",790,50,0);
        cb.EndText();
    }

    // Funkce vygeneruje jaké bloky jsou potřeba pro sestavení naši stavby. Využije k tomu obrázky bloků, které jsme
    // použili pro jejich zobrazení v UI.
    private void GenerateRequirements(List<BlockHandler> handlers, BaseFont font, PdfContentByte cb, bool layer) {
        int x = 70;
        float scale = 15;
        var images = PrepareBlockIcons(handlers, scale);
        if (layer) {
            FitIconsToPage(images, ref scale, ref x);
        }
        cb.BeginText();
        cb.SetFontAndSize(font, 11);
        PrintRequiredBlocks(images,cb,x,layer);
        cb.EndText();
    }

    // Funkce z vybraných bloků určí počet jednotlivých typů bloků a pro každý typ najde ikonu.
    private Dictionary<Image,int> PrepareBlockIcons(List<BlockHandler> handlers, float scale) {
        var required = handlers
            .Where(block => block.GetHeight() / 1.21 <= limit)
            .GroupBy(block => block.ID)
            .Select(group => new { ID = group.Key, Count = group.Count() })
            .OrderBy(block => block.ID);
        
        var images = new Dictionary<Image,int>();
        foreach (var block in required) {
            Texture2D texture = Resources.Load(blockData.GetName(block.ID)) as Texture2D;
            Image img = Image.GetInstance(texture.EncodeToPNG());
            img.ScalePercent(scale);
            images.Add(img,block.Count);
        }
        return images;
    }

    // Funkce vypočítá celkovou velikost všech ikon dohromady a zbytek volného místa.
    // Z těchto hodnot pak určí kde bude začínat první ikona, aby byly obrázky vycentrovány
    // a v případě, že by se nevlezli na stránku, určí o jak moc se mají ikony změnšit, tak
    // aby se vlezli na jeden řádek.
    private static void FitIconsToPage(Dictionary<Image,int> images, ref float scale, ref int x) {
        const int margin = 70;
        int totalImageWidth = images.Sum(image => (int)image.Key.ScaledWidth);
        float remainingSpace = PageSize.A4.Height - 15 * (images.Count - 1);
        
        if (totalImageWidth > remainingSpace - margin) {
            scale /= totalImageWidth / (remainingSpace - margin);
            foreach (var image in images.Keys) {
                image.ScalePercent((int)scale);
            }
            totalImageWidth = images.Sum(image => (int)image.Key.ScaledWidth);
        }
        x = (int)(remainingSpace - totalImageWidth) / 2;
    }

    // Funkce vloží do dokumentu všechny vybrané ikony bloků.
    private void PrintRequiredBlocks(Dictionary<Image, int> images, PdfContentByte cb, int x, bool layer) {
        int y = layer ? 50 : 480;
        foreach (var img in images) {
            img.Key.SetAbsolutePosition(x,y);
            cb.AddImage(img.Key);
            cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER,$"x{img.Value}",x + img.Key.ScaledWidth / 2,y - 10,0);
            x = x + (int)img.Key.ScaledWidth + 15;
            if (!layer && x > PageSize.A4.Height - 120) {
                x = 70;
                y = y - (int)img.Key.ScaledHeight - 50;
            }
        }
    }

    // Funkce vygeneruje stránku, na které bude zobrazeno jak sestavit naši stavbu vrstvu po vrstvě (dle výšky)
    // a k tomu potřebné bloky na dané vrstvě.
    private void GenerateLayers(Document document, BaseFont font, PdfContentByte cb) {
        var blocks = placer.GetBlocks();
        ChangeBlockState(blocks,false,false);

        for (int i = 0; i < buildHeight; i++) {
            var layer = blocks.Where(block => block.GetHeight() == 1.21f * i).ToList();
            if (layer.Count == 0) continue;
            document.NewPage();
            MarkPage(font,cb,i);
            ChangeBlockState(layer,false,true);
            
            InsertScreenshot(cb);
            
            GenerateRequirements(layer,font,cb,true);
            ChangeBlockState(layer,true,true);
        }
        ChangeBlockState(blocks,true,false);

        var aboveLimit = placer.GetBlocks().Where(block => block.GetHeight() * 1.21 > limit).ToList();
        ChangeBlockState(aboveLimit,false,true);
    }

    private void InsertScreenshot(PdfContentByte cb) {
        Image img = Image.GetInstance(TakeScreenshot());
        img.ScalePercent(50);
        int offset = 50;
        if (buildHeight > 5) {
            offset -= 15 * (int)(buildHeight / 4);
        }
        img.SetAbsolutePosition((PageSize.A4.Height - img.ScaledWidth) / 2, (PageSize.A4.Width - img.ScaledHeight) / 2 + offset);
        cb.AddImage(img);
    }

    // Funkce stránku označí číslem představující krok instrukce.
    private static void MarkPage(BaseFont font, PdfContentByte cb, int page) {
        BaseColor color = new BaseColor(136, 195, 82);
        cb.SetColorStroke(color);
        cb.SetColorFill(color);
        cb.Circle(70,515,45);
        cb.FillStroke();
        cb.Stroke();
        
        cb.BeginText();
        cb.SetFontAndSize(font, 42);
        cb.SetColorFill(BaseColor.WHITE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER,$"{page + 1}",70,500,0);
        cb.EndText();
        
        cb.SetColorFill(BaseColor.BLACK);
    }

    // Buď schová/zobrazí vybrané bloky a nebo jim změní barvu.
    private static void ChangeBlockState(List<BlockHandler> blocks, bool saturation, bool value) {
        foreach (var block in blocks) {
            if (saturation) {
                block.DesaturateColor(value);
            }
            else {
                block.gameObject.SetActive(value);
            }
        }
    }
    
    // Funkce vypne hlavní kemru a nahradí ji ortografickým pohledem na naši stavbu což je vhodné pro 2D zobrazení
    // naši stavby. Pak si uloží render z tohoto pohledu a vrátí jej jako proud bytu představující PNG obrázek.
    private byte[] TakeScreenshot() {
        const int width = 1920;
        const int height = 940;
        worldCamera.gameObject.SetActive(false);
        screenshotCamera.gameObject.SetActive(true);
        SetOrthographicSize(screenshotCamera);
        screenshotCamera.backgroundColor = Color.clear;
        
        RenderTexture render = new RenderTexture(width, height, 24);
        screenshotCamera.targetTexture = render;
        
        screenshotCamera.Render();
        Texture2D screenshotTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        RenderTexture.active = render;
        screenshotTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshotTexture.Apply();
        RenderTexture.active = null;
        
        var png = screenshotTexture.EncodeToPNG();

        screenshotCamera.targetTexture = null;
        Destroy(render);
        Destroy(screenshotTexture);

        screenshotCamera.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);
        return png;
    }

    // Změní ortografickou vzdálenost kamery od desky, podle toho jaká je velikost desky.
    private void SetOrthographicSize(Camera cam) {
        cam.orthographicSize = Mathf.RoundToInt(boardSize * 0.5f) + 2;
        if (buildHeight > 5) {
            cam.orthographicSize += buildHeight / 2 - 2 + (buildHeight / 2 - 3);
        }
    }
}