using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UI_Toolkit.Controllers {
    public class UIBlockMenu {
        private readonly Button blockPicker;
        private readonly BlockPlacer placer;
        private readonly List<Button> buttons;
        public UIColorMenu colorMenu;

        private UIEditorManager ui;

        public UIBlockMenu(UIEditorManager ui, Button blockPicker, VisualElement window) {
            this.ui = ui;
            placer = ui.GetPlacer();
            this.blockPicker = blockPicker;
            colorMenu = ui.GetColorMenu();

            buttons = window.Query<Button>(className: "button").ToList();
            int i = 0;
            foreach (var button in buttons) {
                int id = i;
                button.clicked += () => ButtonChangeBlock(id, button.resolvedStyle.backgroundImage);
                i++;
            }
        }

        // Každé tlačítko má v sobě informaci o ID bloku. Podle něj předá informaci o změně bloku v BlockPlacer
        // a změní ikonu aktuálního bloku.
        private void ButtonChangeBlock(int id, StyleBackground image) {
            placer.ChangeBlock(id);
            blockPicker.style.backgroundImage = image;
            ChangeColorButtonState(id);
            ui.SwitchMode(Mode.Builder);
        }

        // Podle předaného ID nastaví ikonu aktuálního bloku.
        public void ChangeIcon(int id) {
            blockPicker.style.backgroundImage = buttons[id].resolvedStyle.backgroundImage;
            ChangeColorButtonState(id);
            ui.SwitchMode(Mode.Builder);
        }

        // Pokud nastavujeme aktuální blok na blok který může být jen bílý, musíme
        // o tom předat informaci menu barev.
        public void ChangeColorButtonState(int id) {
            if (placer.GetMode() == Mode.Color) return;
            if (id is >= 0 and < 4) {
                colorMenu.DisableColors();
            }
            else {
                colorMenu.EnableColors();
            }
        }
    }
}
