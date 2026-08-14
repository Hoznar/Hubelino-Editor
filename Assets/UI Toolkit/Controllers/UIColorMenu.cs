using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI_Toolkit.Controllers {
    public class UIColorMenu {
        private readonly Button colorPicker;
        private readonly BlockPlacer placer;
        private readonly List<Button> buttons;
        
        private int colorIndex;
        private readonly Color32 whiteColor = new(233, 233, 233, 255);

        public UIColorMenu(UIEditorManager ui, Button colorPicker) {
            placer = ui.GetPlacer();
            this.colorPicker = colorPicker;
            
            VisualElement root = ui.GetComponent<UIDocument>().rootVisualElement;
            buttons = new List<Button> {
                root.Q<Button>("Red"),
                root.Q<Button>("Blue"),
                root.Q<Button>("Green"),
                root.Q<Button>("Yellow"),
            };

            foreach (var button in buttons) {
                button.clicked += () => ProcessButtonColor(button);
            }
        }
        
        // Při zmáčknutí klávesové zkratky změní aktuální barvu na následující.
        public void CycleColors() {
            if (colorPicker.pickingMode == PickingMode.Ignore) return;
            ProcessButtonColor(buttons[(colorIndex + 1) % buttons.Count]);
        }

        // Funkce předá BlockPlacer informaci o své barvě, čímž se danou barvou obarví pokládané bloky.
        // Ještě se změní ikona aktuální barvy.
        private void ProcessButtonColor(Button button) {
            colorIndex = buttons.IndexOf(button);
            StyleColor color = button.resolvedStyle.backgroundColor;
            ChangeButtonColor(color);
            placer.ChangeColor(color.value);
        }
        
        // Aby tlačítko nezablikalo při změně barvy, musíme na chvíly vypnout předchodovou animaci.
        private void ChangeButtonColor(StyleColor color) {
            var style = colorPicker.style;
            var duration = style.transitionDuration;
            
            style.transitionDuration = new StyleList<TimeValue>(0);
            style.backgroundColor = color;
            style.transitionDuration = duration;
        }
        
        // Funkce vypne možnost změnit aktuální barvu, budeme moct pouze pokládat bílé bloky.
        public void DisableColors() {
            if (colorPicker.pickingMode == PickingMode.Ignore) return;
            colorPicker.pickingMode = PickingMode.Ignore;
            ChangeButtonMode(false);

            ChangeButtonColor(colorPicker.resolvedStyle.backgroundColor - new Color(0, 0, 0, 0.5f));
            placer.ChangeColor(whiteColor);
        }
        
        // Funkce opět umožní změnit aktuální barvu.
        public void EnableColors() {
            if (colorPicker.pickingMode == PickingMode.Position) return;
            colorPicker.pickingMode = PickingMode.Position;
            ChangeButtonMode(true);
            
            ChangeButtonColor(colorPicker.resolvedStyle.backgroundColor + new Color(0, 0, 0, 0.5f));
            placer.ChangeColor(colorPicker.resolvedStyle.backgroundColor);
        }

        // Ve velmi specifické situaci se může stát že uživatel může úmyslně rozbít program tak,
        // že změní barvu bloku který může být jen bílý. Proto je pro jistotu lepší vypnout i schovaná tlačítka.
        private void ChangeButtonMode(bool value) {
            foreach (var button in buttons) {
                button.pickingMode = value ? PickingMode.Position : PickingMode.Ignore;
            }
        }
    }
}
