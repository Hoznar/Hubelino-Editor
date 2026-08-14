using UI_Toolkit.Controllers;
using UnityEngine;

public class Shortcuts : MonoBehaviour {
    private BlockPlacer placer;
    private UIEditorManager ui;
    private int colorIndex;
    
    void Start() {
        placer = FindObjectOfType<BlockPlacer>();
        ui = FindObjectOfType<UIEditorManager>();
    }
    
    void Update() {
        if (Input.GetKeyDown("c")) {
            ui.GetColorMenu().CycleColors();
        }
        else if (Input.GetKeyDown("x") || (!Input.GetKey(KeyCode.LeftControl) && Input.GetAxis("Mouse ScrollWheel") > 0)) {
            placer.RotateBlock(true);
        }
        else if (Input.GetKeyDown("z") || (!Input.GetKey(KeyCode.LeftControl) && Input.GetAxis("Mouse ScrollWheel") < 0)) {
            placer.RotateBlock(false);
        }
        else if (Input.GetKeyDown("a")) {
            placer.CycleBlocks(false);
            ui.GetBlockMenu().ChangeIcon(placer.blockIndex);
        }
        else if (Input.GetKeyDown("d")) {
            placer.CycleBlocks(true);
            ui.GetBlockMenu().ChangeIcon(placer.blockIndex);
        }
        else if (Input.GetKeyDown("1")) {
            ui.SwitchMode(Mode.Builder);
        }
        else if (Input.GetKeyDown("2")) {
            ui.SwitchMode(Mode.Remover);
        }
        else if (Input.GetKeyDown("3")) {
            ui.SwitchMode(Mode.Color);
        }
        else if (Input.GetKeyDown(KeyCode.Escape)) {
            ui.GetMenu().OpenSettings();
        }
    }
}
