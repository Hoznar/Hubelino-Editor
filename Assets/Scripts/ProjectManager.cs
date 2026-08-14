using System;
using System.Collections.Generic;
using System.Linq;
using UI_Toolkit.Controllers;
using UnityEngine;

public class ProjectManager : MonoBehaviour {
    private static ProjectManager instance;
    private BlockPlacer placer;
    private CameraHandler cam;
    private UIEditorManager ui;
    private Files files;

    private string projectName;
    private int boardLength;
    private int boardWidth;
    
    [SerializeField] private GameObject cell;
    [SerializeField] private Transform center;
    [SerializeField] private Transform parent;

    private Vector3 centerPosition;
    
    void Awake() {
        instance = this;
        placer = FindObjectOfType<BlockPlacer>();
        cam = FindObjectOfType<CameraHandler>();
        ui = FindObjectOfType<UIEditorManager>();
        files = FindObjectOfType<Files>();
        centerPosition = center.position;
    }

    // Vymaže vše z aktuálního projektu a vytvoří nový projekt pomocí zadaných parametrů.
    public void CreateNewProject(string project, int length, int width) {
        ResetProject();
        projectName = project;
        boardLength = length;
        boardWidth = width;
        GameObject board = CreateBoard();
        cam.SetCameraDistance(GetAverageSize(length,width));
        cam.SetCameraBounds(board);
    }

    // Funkce vymaže desku a následně všechny bloky.
    private void ResetProject() {
        if (parent.transform.childCount != 0) {
            GameObject currentBoard = parent.transform.GetChild(0).gameObject;
            // Destroy se volá později, takže musíme označit desku jako neaktivní.
            // Jinak by se vytváření nové desky spojilo s aktuální deskou.
            currentBoard.SetActive(false);
            Destroy(currentBoard);
        }
        placer.DestroyAllBlocks();
    }

    // Vytvoří novou desku pomocí menších buňek, ze kterých vytvoří samostatný objekt.
    // Dále objketu nastaví collider a upraví oblast na kterou lze pokládat bloky.
    private GameObject CreateBoard() {
        int length = boardLength / 2;
        int width = boardWidth / 2;
        for (int i = -length; i < length; i++) {
            for (int j = -width + 1; j < width + 1; j++) {
                Vector3 position = new Vector3(centerPosition.x + i, -0.055f, centerPosition.z + j);
                Instantiate(cell, position, Quaternion.identity, parent);
            }
        }
        GameObject board = MergeBoard();
        board.transform.parent = parent;
        SetBoardCollider(board);
        Grid.SetGridSize(board);
        return board;
    }

    // Funkce si zjistí mesh všech buňek desky. Vytvoří nový objekt do kterého se spojí všechny
    // buňky desky. Také je potřeba nastavit desce parametry pro správnou funkčnost BlockPlacer.
    private GameObject MergeBoard() {
        MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        
        for (int i = 0; i < meshFilters.Length; i++) {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }
        
        GameObject mergedObject = new GameObject() {
            tag = "Board",
            layer = 6
        };
        var filter = mergedObject.AddComponent<MeshFilter>();
        var render = mergedObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        
        filter.mesh = mesh;
        filter.mesh.CombineMeshes(combine);
        render.materials = parent.GetComponentInChildren<MeshRenderer>().materials;
        
        foreach (Transform child in parent) {
            Destroy(child.gameObject);
        }
        return mergedObject;
    }

    // Přidá nové desce BoxCollider o vhodné veliksoti.
    private static void SetBoardCollider(GameObject board) {
        BoxCollider boxCollider = board.AddComponent<BoxCollider>();
        Vector3 boxCenter = boxCollider.center;
        Vector3 boxSize = boxCollider.size;

        boxCollider.center = new Vector3(boxCenter.x, -0.023f, boxCenter.z);
        boxCollider.size = new Vector3(boxSize.x, 0.062f, boxSize.z);
    }
    
    // Funkce získá všechny informace o ukládaném projektu a přepošle je skriptu Files.
    public void SaveProject() {
        var project = new Project {
            projectName = projectName,
            boardLength = boardLength,
            boardWidth = boardWidth
        };
        foreach (var handler in placer.GetBlocks()) {
            if (handler.IsLevitating()) {
                ui.ShowMessage("Cannot save file. Some blocks are levitating.",true);
                return;
            }
            GameObject obj = handler.gameObject;
            Block block = new Block(handler.ID, obj.transform.position, obj.transform.eulerAngles.y, handler.GetColor());
            project.Add(block);
        }
        
        if (files.SaveToFile(project)) {
            ui.ShowMessage("Saved.",false);
        }
        else {
            ui.ShowMessage("Project could not be saved.", true);
        }
    }

    // Funkce zařídí aby skript Files načetl potřebné informace a předáme BlockPlacer bloky na položení.
    public bool LoadProject() {
        var project = files.LoadFromFile();
        if (project == null) return false;
        
        CreateNewProject(project.projectName,project.boardLength,project.boardWidth);
        placer.PlaceBlocks(project.blocks);
        ui.ShowMessage("Loaded.",false);
        return true;
    }

    // Zjistí jaké bloky jsou na desce a předá je skriptu Files, který z nich vytvoří instrukce.
    public void GenerateInstructionsFromProject() {
        var blocks = placer.GetBlocks();
        if (blocks.Count == 0 || blocks.Any(block => block.IsLevitating())) {
            ui.ShowMessage("Cannot generate instructions. Board is empty or some blocks are levitating.", true);
            return;
        }
        files.GenerateInstructions(blocks,projectName,GetAverageSize(boardLength,boardWidth));
    }

    // Vypočítá průměrnou velikost desky, aby se správně nastavila vzdálenost kamery
    // a velikost obrázku instrukce při generování.
    private int GetAverageSize(int length, int width) {
        int maxSize = Mathf.Max(length, width);
        int minSize = Mathf.Min(length, width);
        return maxSize - (int)((minSize / (float)maxSize) * (float)(maxSize - minSize));
    }
}

public enum Mode { Builder, Remover, Color }

[Serializable]
public class Block {
    public int id;
    public double[] position;
    public float rotation;
    public Color32 color;

    // Float čísla se po serializaci ukládají velmi nepřesně, takže je lepší uložit vektor jako
    // pole double hodnot. Později z těchto hodnot znovu uděláme vektor float čísel.
    public Block(int id, Vector3 position, float rotation, Color color) {
        this.id = id;
        this.position = new double[3];
        this.position[0] = position.x;
        this.position[1] = Math.Round(position.y, 2, MidpointRounding.AwayFromZero);
        this.position[2] = position.z;
        this.rotation = rotation;
        this.color = color;
    }
}

[Serializable] public class Project {
    public string projectName;
    public int boardLength;
    public int boardWidth;
    public List<Block> blocks;

    public Project() {
        blocks = new List<Block>();
    }

    public void Add(Block block) {
        blocks.Add(block);
    }
}
