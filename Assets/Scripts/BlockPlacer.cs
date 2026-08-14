using System.Collections.Generic;
using System.Linq;
using UI_Toolkit.Controllers;
using Unity.VisualScripting;
using UnityEngine;

public class BlockPlacer : MonoBehaviour {
    private Mode mode = Mode.Builder;
    private bool inFocus = true;
    private const int mask = ~(1 << 8);
    
    public int blockIndex;
    private int blockRotation;
    public Color blockColor;
    public int aboveLimit;

    private Vector3 blockPosition;
    private GameObject ghostBlock;
    private GameObject highlightedBlock;
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform parentObject;
    
    private List<BlockHandler> connectionBuffer;
    private Dictionary<GameObject,BlockHandler> blockCache;
    
    private Grid grid;
    private Reversibility reversibility;
    private BlockDatabase blockData;
    private UIEditorManager ui;
    
    
    public Mode GetMode() {
        return mode;
    }
    
    public bool InFocus() {
        return inFocus;
    }
    
    // Vrátí všechny handlery bloků které nejsou schované.
    public List<BlockHandler> GetBlocks() {
        return blockCache.Where(block => block.Key.activeSelf).Select(block => block.Value).ToList();
    }

    public void ChangeMode(Mode newMode) {
        mode = newMode;
        ResetBlockParameters();
    }

    public void ChangeColor(Color color) {
        blockColor = color;
    }

    public void RotateBlock(bool direction) {
        if (direction && (blockRotation += 90) > 270) {
            blockRotation = 0;
        }
        else if (!direction && (blockRotation -= 90) < 0) {
            blockRotation = 270;
        }
        ResetBlockParameters();
        UpdateMouseOver();
    }
    
    public void ChangeBlock(int id) {
        blockIndex = id;
        ResetBlockParameters();
    }
    
    public void CycleBlocks(bool next) {
        if (next) {
            blockIndex = (blockIndex + 1) % blockData.GetBlocksCount();
        }
        else {
            if (--blockIndex < 0) {
                blockIndex = blockData.GetBlocksCount() - 1;
            }
        }
        ResetBlockParameters();
        UpdateMouseOver();
    }

    public void FocusOnPlacer(bool focus) {
        inFocus = focus;
        ResetBlockParameters();
    }

    // Načteme potřebné objekty.
    private void Awake() {
        grid = FindObjectOfType<Grid>();
        reversibility = FindObjectOfType<Reversibility>();
        blockData = FindObjectOfType<BlockDatabase>();
        connectionBuffer = new List<BlockHandler>();
        blockCache = new Dictionary<GameObject, BlockHandler>();
        ui = FindObjectOfType<UIEditorManager>();
    }

    // Zjistí jestli nedošlo k nějaké události
    private void Update() {
        if (inFocus && !Input.anyKey && (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)) {
            UpdateMouseOver();
        }
        else if (inFocus && Input.GetMouseButtonDown(0)) {
            UpdateMouseInput();
        }
    }

    // Pokud došlo ke kliknutí a blok není zablokovaný, na místo se vytvoří skutečný blok.
    // Pokud jsme v režimu mazání, blok se po kliknutí schová.
    // Pokud jsme v režimu barvení, zkontroluje se jestli se daný blok dá přebarvit a přebarví jej.
    private void UpdateMouseInput() {
        if (Physics.Raycast(worldCamera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity, mask)) {
            if (mode == Mode.Builder && ghostBlock) {
                if (ghostBlock.CompareTag("Blocked")) return;
                Destroy(ghostBlock);
                GameObject obj = PlaceBlock(false);
                reversibility.PushToUndo(new Operation(0,blockCache[obj],blockColor,obj));
                UpdateMouseOver();
            }
            else if (mode == Mode.Remover && GetDefaultObject(hit.collider).CompareTag("Block")) {
                GameObject obj = hit.collider.gameObject.transform.parent.gameObject;
                reversibility.PushToUndo(new Operation(1,blockCache[obj],blockColor,obj));
                HideBlock(obj);
            }
            else if (mode == Mode.Color && GetDefaultObject(hit.collider).CompareTag("Block")) {
                GameObject obj = highlightedBlock.transform.parent.gameObject;
                if (!blockCache[obj].CanRecolor()) return;
                reversibility.PushToUndo(new Operation(2,blockCache[obj],blockColor,obj));
                RecolorBlock(obj,blockColor,true);
            }
        }
    }
    
    // Pokud došlo k pohybu myší, vytvoří se u kurzoru průhledný blok v případě, že jsme aktuálně v režimu stavby.
    // Pokud jsme v režimu odstraňování, blok na kterém máme kurzor se zvýrazní.
    private void UpdateMouseOver() {
        if (Physics.Raycast(worldCamera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity, mask)) {
            if (mode == Mode.Builder) {
                Vector3 newPosition = grid.SnapToGrid(hit.point, blockData.GetSize(blockIndex), blockRotation);
                if (GetDefaultObject(hit.collider).CompareTag("Block")) {
                    newPosition = MoveOutOfBlock(newPosition, hit);
                }
                if (blockPosition != newPosition) {
                    blockPosition = newPosition;
                    Destroy(ghostBlock);
                    connectionBuffer.Clear();
                    PlaceBlock(true);
                }
            }
            else {
                HighlightBlock(GetDefaultObject(hit.collider));
            }
        }
        else {
            ResetBlockParameters();
        }
    }
    
    // Pokud jsme najeli myší na blok, musí se nový blok posunout o část své délky, aby se bloky neprolínaly.
    private Vector3 MoveOutOfBlock(Vector3 position, RaycastHit hit) {
        Vector3 size = blockData.GetSize(blockIndex) / 2;
        if (blockRotation is 90 or 270) {
            (size.x, size.z) = (size.z, size.x);
        }
        if ((int)hit.normal.x != 0) {
            position.x = Grid.FloorValue(hit.point.x);
            position.x += size.x * hit.normal.x;
        }
        else if ((int)hit.normal.z != 0) {
            position.z = Grid.FloorValue(hit.point.z);
            position.z += size.z * hit.normal.z;
        }
        return position;
    }

    // Vrátí první podobjekt daného bloku podle předaného collideru.
    private static GameObject GetDefaultObject(Collider collision) {
        return collision.gameObject.transform.parent.GetChild(0).gameObject;
    }

    // Při změně režimu resetuje některé proměnné.
    public void ResetBlockParameters() {
        if (ghostBlock) {
            Destroy(ghostBlock);
            connectionBuffer.Clear();
            blockPosition = new Vector3();
        }
        if (highlightedBlock) {
            blockCache[highlightedBlock.transform.parent.gameObject].UnHighlightBlock();
            highlightedBlock = null;
        }
    }
    
    // Když jsme v režimu odstraňování, při nájezdu myší na blok se daný blok zvýrazní.
    // Po přemístění kurzoru na jiný objekt se vrátí do původního stavu.
    private void HighlightBlock(GameObject obj) {
        if (highlightedBlock != obj) {
            if (highlightedBlock) {
                blockCache[highlightedBlock.transform.parent.gameObject].UnHighlightBlock();
            }
            if (obj.CompareTag("Block")) {
                highlightedBlock = obj;
                blockCache[highlightedBlock.transform.parent.gameObject].HighlightBlock();
            }
            else {
                highlightedBlock = null;
            }
        }
    }

    // Při smazání bloku se daný blok nejprve skryje, aby se mohl zase jednoduše objevit bez
    // nutnosti jej znovu inicializovat, když budeme chtít danou operaci vrátit.
    // Je ale nutné blok odpojit od ostatních po tuto dobu, a dalé skrýt všechny konektory, kvůli tomu
    // jak je naprogramovaná funkce na nalezení propojení.
    public void HideBlock(GameObject obj) {
        BlockHandler handler = blockCache[obj];
        handler.DisconnectBlock();
        handler.connectedBlocks.Clear();
        foreach (Transform child in obj.transform) {
            child.gameObject.SetActive(false);
        }
        obj.SetActive(false);
        if (handler.IsAboveLimit()) {
            aboveLimit--;
            ui.ToggleWarning(aboveLimit > 0);
        }
    }

    // Při zavolání funkce se znovu objeví blok, který byl předtím schován. Musí znovu zjisit, jaké bloky
    // jsou kolem něj a propojit je mezi sebou. Nazávěr musíme vrátit konektory do původního stavu.
    public void ShowBlock(GameObject obj) {
        obj.SetActive(true);
        connectionBuffer.Clear();
        ValidateAndConnect(obj, obj.transform.position.y == 0,false);
        foreach (Transform child in obj.transform) {
            child.gameObject.SetActive(true);
        }
        blockCache[obj].ConnectBlocks(connectionBuffer);
        if (blockCache[obj].IsAboveLimit()) {
            aboveLimit++;
            ui.ToggleWarning(aboveLimit > 0);
        }
    }

    // Funkce zruší propojení mezi bloky a kompletně smaže daný blok.
    public void DestroyBlock(GameObject obj) {
        blockCache[obj].DisconnectBlock();
        blockCache.Remove(obj);
        Destroy(obj);
    }
    
    // Funkce přebarví daný blok na jinou barvu.
    public void RecolorBlock(GameObject obj, Color color, bool highlight) {
        BlockHandler handler = blockCache[obj];
        handler.SetNewColor(color,highlight);
    }

    // Zajistí aby blok nebyl mimo desku a pak vytvoří blok. Také ho otočí o nastavenou rotaci.
    private GameObject PlaceBlock(bool ghost) {
        if (Grid.OutOfBounds(ref blockPosition, blockData.GetSize(blockIndex), blockRotation)) return null;
        
        GameObject obj = Instantiate(blockData.GetBlock(blockIndex), blockPosition, Quaternion.identity, parentObject);
        obj.transform.Rotate(0,blockRotation,0,Space.Self);
        if (ghost) {
            SetGhostParameters(obj);
        }
        else {
            SetBlockParameters(obj,blockIndex,blockColor);
        }
        return obj;
    }

    // Nastaví potřebné parametry bloku pro správnou funkčnost. Bloku nastavíme zvolenou barvu,
    // id a propojíme s ostatními bloky.
    private void SetBlockParameters(GameObject obj, int id, Color color) {
        Transform block = obj.transform.GetChild(0);
        foreach (Transform child in obj.transform) {
            child.gameObject.SetActive(true);
        }
        BlockHandler handler = block.AddComponent<BlockHandler>();
        handler.SetID(id);
        handler.SetNewColor(color,false);
        handler.ConnectBlocks(connectionBuffer);
        blockCache.Add(obj,handler);

        if (handler.IsAboveLimit()) {
            aboveLimit++;
            ui.ToggleWarning(aboveLimit > 0);
        }
    }

    // Průhlednému bloku nastavíme průhledný materiál, a ověříme,
    // zda nenastane kolize s jiným blokem. Pokud ano, blok se zbarví na červeno a zablokuje se aby nešel položit.
    private void SetGhostParameters(GameObject obj) {
        Transform block = obj.transform.GetChild(0);
        MeshRenderer render = block.GetComponent<MeshRenderer>();
        render.material = ghostMaterial;
        if (ValidateAndConnect(obj,blockPosition.y == 0,true)) {
            obj.tag = "Blocked";
            render.material.SetColor("_Color",new Color(1f,0.5f,0.5f,0.5f));
        }
        ghostBlock = obj;
    }

    // Funkce prvně pro každý BoxCollider daného bloku zkontroluje, jestli se na daném místě neprolíná s jiným colliderem.
    // Pokud ano, nemůžeme blok položit. Dále skrz každý propojující collider získá všechny bloky které se nachází pod
    // nebo nad daným blokem a uloží si o nich informace do bufferu. Pokud je blok připojený přímo k desce, nemusíme
    // kontrolovat co je pod ním. Pokud blok k ničemu nebude připojený, nepůjde položit, aby nelevitoval.
    private bool ValidateAndConnect(GameObject obj, bool onGround, bool checkCollisions) {
        for (int i = 1; i < obj.transform.childCount; i++) {
            GameObject child = obj.transform.GetChild(i).gameObject;
            child.SetActive(true);
            Bounds bounds = child.GetComponent<BoxCollider>().bounds;
            child.SetActive(false);

            if (checkCollisions && !child.name.Contains("Connector") && Physics.CheckBox(bounds.center, bounds.extents * 0.9f)) {
                connectionBuffer.Clear();
                return true;
            }
            if (!onGround && (child.CompareTag("Connector") || child.CompareTag("ConnectorBottom"))) {
                ConnectSurroundingBlocks(bounds,0);
            }
            if (child.CompareTag("Connector") || child.CompareTag("ConnectorTop")) {
                ConnectSurroundingBlocks(bounds,1);
            }
        }
        return connectionBuffer.Count == 0 && !onGround;
    }

    // Funkce zjistí jaké bloky jsou pod nebo nad daným blokem a uloží si o nich informaci.
    // Pokud se rozhodneme daný blok později vytvořit, bloky se navzájem propojí.
    private void ConnectSurroundingBlocks(Bounds bounds, int direction) {
        const float height = 0.605f;
        Vector3 pos = bounds.center;
        Vector3 ext = bounds.extents;
        ext.y = height;
        if (direction == 0) {
            pos.y = pos.y - bounds.extents.y - height;
        }
        else {
            pos.y = pos.y + bounds.extents.y + height;
        }
        foreach (var collision in Physics.OverlapBox(pos,ext * 0.9f)) {
            GameObject obj = collision.gameObject;
            if (obj.CompareTag("Untagged")) continue;
            switch (direction) {
                case 0 when !obj.CompareTag("ConnectorBottom"):
                case 1 when !obj.CompareTag("ConnectorTop"):
                    connectionBuffer.Add(blockCache[collision.transform.parent.gameObject]);
                    break;
            }
        }
    }

    // Položí všechny bloky z listu. Předtím smaže všechny bloky co jsou na desce.
    public void PlaceBlocks(List<Block> blocks) {
        foreach (var block in blocks) {
            Vector3 position = new Vector3((float)block.position[0], (float)block.position[1], (float)block.position[2]);
            GameObject obj = Instantiate(blockData.GetBlock(block.id), position, Quaternion.identity, parentObject);
            obj.transform.Rotate(0,block.rotation,0,Space.Self);
            ValidateAndConnect(obj, obj.transform.position.y == 0,false);
            SetBlockParameters(obj,block.id,block.color);
            connectionBuffer.Clear();
        }
    }
    
    // Vymaže všechny bloky na desce.
    public void DestroyAllBlocks() {
        reversibility.Clear();
        connectionBuffer.Clear();
        blockCache.Clear();
        aboveLimit = 0;
        ui.ToggleWarning(false);
        foreach (Transform child in parentObject.transform) {
            GameObject obj = child.gameObject;
            // Objekt se reálně zruší až po aktuálním Update, což by rozhodilo připojování bloků, protože se pořád bere,
            // že jsou na desce. Z toho důvodu je musíme shovat, než se smažou.
            obj.gameObject.SetActive(false);
            Destroy(obj.gameObject);
        }
    }
}

