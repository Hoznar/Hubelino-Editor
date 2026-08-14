using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class BlockHandler : MonoBehaviour {
    public int ID { get; private set; }
    private Color setColor;
    private Material material;

    private bool marked;
    public List<BlockHandler> connectedBlocks;

    public void Awake() {
        material = GetComponent<MeshRenderer>().material;
        setColor = material.color;
        connectedBlocks = new List<BlockHandler>();
    }

    public Color GetColor() {
        return setColor;
    }

    public float GetHeight() {
        return gameObject.transform.parent.position.y;
    }

    public bool IsAboveLimit() {
        return gameObject.transform.parent.position.y > 1.21 * 14;
    }

    public bool IsLevitating() {
        return marked;
    }
    
    public void SetID(int id) {
        ID = id;
    }

    /* Pokud blok levituje, blok se označí. Pokud ne, nastavíme původní barvu. */
    private void ToggleMark() {
        marked = !marked;
        if (marked) {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.red);
            material.SetColor("_Color",Color.red);
        }
        else {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_Color",setColor);
        }
    }

    public bool CanRecolor() {
        return ID >= 4 && !marked;
    }

    // Změní barvu bloku a zvýrazní daný blok.
    // Pokud je zvolená barva bílá, ověří že se dá blok nabarvit na bílo.
    public void SetNewColor(Color color, bool highlight) {
        setColor = color;
        if (marked) return;
        material.color = setColor;
        if (highlight) {
            HighlightBlock();
        }
    }

    public void HighlightBlock() {
        if (!marked) {
            material.color = new Color(setColor.r * 1.6f, setColor.g * 1.6f, setColor.b * 1.6f);
        }
    }

    public void UnHighlightBlock() {
        if (!marked) {
            material.color = setColor;
        }
    }

    // Funkce dočasně změni barvu bloku tak, aby měla menší saturaci.
    public void DesaturateColor(bool desaturate) {
        if (desaturate) {
            Color.RGBToHSV(setColor, out float h, out float s, out float v);
            if (v < 0.8f) v += 0.2f;
            if (s == 0f) v -= 0.2f;
            material.color = Color.HSVToRGB(h, s * 0.25f, v);
        }
        else {
            material.color = setColor;
        }
    }

    private void ConnectBlock(BlockHandler block) {
        connectedBlocks.Add(block);
    }

    /* Funkce navzájem propojí bloky a pokud skrze nový blok existuje cesta k desce,
       připojené levitující bloky se odoznačí. */
    public void ConnectBlocks(List<BlockHandler> blocks) {
        bool pathToGroundExists = false;
        connectedBlocks.AddRange(blocks);
        foreach (var block in blocks) {
            block.ConnectBlock(this);
            if (!block.marked) {
                pathToGroundExists = true;
            }
        }
        if (pathToGroundExists || gameObject.transform.position.y == 0) {
            foreach (var block in connectedBlocks.Where(block => block.marked)) {
                block.UnmarkBlock();
            }
        }
        else {
            ToggleMark();
        }
    }

    /* Funkce rekurzivně odoznačí bloky. */
    private void UnmarkBlock() {
        ToggleMark();
        foreach (var block in connectedBlocks.Where(block => block.marked)) {
            block.UnmarkBlock();
        }
    }

    /* Funkce odpojí bloky a pokud kvůli odstranění daného bloku přestane existovat cesta
       k desce, označí bloky, které budou levitovat. */
    public void DisconnectBlock() {
        foreach (var block in connectedBlocks) {
            block.DisconnectBlock(this);
        }
        if (marked) {
            ToggleMark();
        }
        MarkIfLevitating();
    }

    private void DisconnectBlock(BlockHandler block) {
        connectedBlocks.Remove(block);
    }

    /* Funkce si pro každý blok, který byl připojen k danému bloku, zkontroluje jestli existuje cesta k desce a pokud
       ne, tak všechny bloky označí. */
    private void MarkIfLevitating() {
        var visited = new List<BlockHandler>();
        foreach (var block in connectedBlocks) {
            if (!block.marked && !FindPathToGround(block, visited)) {
                foreach (var visitedBlock in visited) {
                    visitedBlock.ToggleMark();
                }
            }
            visited.Clear();
        }
    }

    /* Funkce rekurzivně prochází připojené bloky, značí si ty, kterými jsme už prošli a kontroluje,
      jestli jsme v nulové výšce, tedy že je blok připojen k desce. */
    private bool FindPathToGround(BlockHandler block, List<BlockHandler> visited) {
        if (visited.Contains(block)) return false;
        visited.Add(block);
        if (block.gameObject.transform.parent.position.y == 0) return true;
        foreach (var connected in block.connectedBlocks) {
            if (FindPathToGround(connected, visited)) {
                return true;
            }
        }
        return false;
    }
}
