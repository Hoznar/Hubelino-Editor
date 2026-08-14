using System.Collections.Generic;
using System.Linq;
using UI_Toolkit.Controllers;
using UnityEngine;

// Třída ve které uchováváme informace potřebné k tomu abychom mohli provádět operace Undo a Redo.
public class Operation {
    public int op;
    public Color color;
    public Color newColor;
    public GameObject obj;

    public Operation(int op, BlockHandler handler, Color newColor, GameObject obj) {
        this.op = op;
        color = handler.GetColor();
        this.newColor = newColor;
        this.obj = obj;
    }
}

public class Reversibility : MonoBehaviour {
    private List<Operation> undoStack;
    private List<Operation> redoStack;

    private BlockPlacer placer;
    private UIEditorManager ui;

    void Start() {
        undoStack = new List<Operation>(10);
        redoStack = new List<Operation>(10);
        placer = FindObjectOfType<BlockPlacer>();
        ui = FindObjectOfType<UIEditorManager>();
    }
    
    // Stejně jako na klasický zásobník se přidá položka, jenom s tím rozdílem, že si udržujeme
    // danou kapacitu, a pokud ji přesáhneme, smažeme položku na spodu zásobníku.
    private void Push(List<Operation> stack, Operation op) {
        if (stack.Count == stack.Capacity) {
            GameObject obj = stack[0].obj;
            if (obj && !obj.activeSelf && LastOfKind(obj)) {
                placer.DestroyBlock(obj);
            }
            stack.RemoveAt(0);
        }
        stack.Add(op);
    }

    private bool LastOfKind(GameObject obj) {
        var first = undoStack.FindAll(op => op.obj == obj);
        var second = redoStack.FindAll(op => op.obj == obj);
        return first.Count + second.Count == 1;
    }


    // Funkce vrátí položku z vrcholu zásobníku.
    private Operation Pop(List<Operation> stack) {
        Operation op = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return op;
    }

    // Funkce přidá operaci na Undo zásobník. Pokud jsou nějaké operace
    // v Redo, všechny se smažou.
    public void PushToUndo(Operation operation, bool fromRedo = false) {
        if (!fromRedo && redoStack.Count != 0) {
            DestroyRedoBlocks();
            ui.ToggleReverseButton(false);
        }
        if (undoStack.Count == 0) {
            ui.ToggleReverseButton(true);
        }
        Push(undoStack,operation);
    }

    // Všechny schované bloky se kompletně smažou.
    private void DestroyRedoBlocks() {
        foreach (var obj in redoStack.Select(operation => operation.obj).Distinct().ToList().Where(obj => obj && !obj.activeSelf)) {
            if (undoStack.FindAll(op => op.obj == obj).Count == 0) {
                placer.DestroyBlock(obj);
            }
        }
        redoStack.Clear();
    }

    // Funkce odvolá poslední provedenou operaci a přidá ji na Redo zásobník.
    public void Undo() {
        Operation operation = Pop(undoStack);
        if (undoStack.Count == 0) {
            ui.ToggleReverseButton(true);
        }
        switch (operation.op) {
            case 0:
                placer.HideBlock(operation.obj);
                break;
            case 1:
                placer.ShowBlock(operation.obj);
                break;
            default:
                placer.RecolorBlock(operation.obj, operation.color,false);
                (operation.color, operation.newColor) = (operation.newColor, operation.color);
                break;
        }
        PushToRedo(operation);
    }

    // Funkce přidá operaci na Redo zásobník.
    private void PushToRedo(Operation operation) {
        if (redoStack.Count == 0) {
            ui.ToggleReverseButton(false);
        }
        Push(redoStack, operation);
    }

    // Funkce znovu provede poslední operaci kterou jsme odvolali a přidá ji
    // na Undo zásobnik.
    public void Redo() {
        Operation operation = Pop(redoStack);
        if (redoStack.Count == 0) {
            ui.ToggleReverseButton(false);
        }
        switch (operation.op) {
            case 0:
                placer.ShowBlock(operation.obj);
                break;
            case 1:
                placer.HideBlock(operation.obj);
                break;
            default:
                placer.RecolorBlock(operation.obj, operation.color,false);
                (operation.color, operation.newColor) = (operation.newColor, operation.color);
                break;
        }
        PushToUndo(operation,true);
    }

    // Kompletně vyprázdní zásobníky.
    public void Clear() {
        if (undoStack.Count != 0) {
            ui.ToggleReverseButton(true);
        }
        if (redoStack.Count != 0) {
            ui.ToggleReverseButton(false);
        }
        undoStack.Clear();
        redoStack.Clear();
    }
}
