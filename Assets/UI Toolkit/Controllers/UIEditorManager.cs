using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UI_Toolkit.Controllers {
    public class UIEditorManager : MonoBehaviour {
        private Button pressedButton;
        private VisualElement activeWindow;
        private VisualElement toast;

        private UIBlockMenu blockMenu;
        private UIColorMenu colorMenu;
        private UIMenuManager menuManager;
        
        private Button buttonUndo;
        private Button buttonRedo;

        private VisualElement warning;
        private bool warningDisplayed;

        private VisualElement root;
        private List<VisualElement> menus;

        private ProjectManager manager;
        private BlockPlacer placer;
        private Reversibility reversibility;

        // Přiradíme elementům požadovanou funkcionalitu
        private void OnEnable() {
            placer = FindObjectOfType<BlockPlacer>();
            reversibility = FindObjectOfType<Reversibility>();
            manager = FindObjectOfType<ProjectManager>();
            root = GetComponent<UIDocument>().rootVisualElement;
            menus = root.Query<VisualElement>("Container").ToList();
            
            toast = root.Q<VisualElement>("Message");
            toast.style.opacity = 0;
            toast.style.transitionProperty = new List<StylePropertyName> { "opacity" };
            toast.style.transitionTimingFunction = new List<EasingFunction> { EasingMode.Ease };

            warning = root.Q<VisualElement>("WarningBox");

            var buttonBuilder = root.Q<Button>("buttonBuilder");
            var buttonRemover = root.Q<Button>("buttonRemover");
            var buttonColor = root.Q<Button>("buttonColor");
            pressedButton = buttonBuilder;

            buttonBuilder.clicked += () => ButtonModePressed(buttonBuilder, Mode.Builder);
            buttonRemover.clicked += () => ButtonModePressed(buttonRemover, Mode.Remover);
            buttonColor.clicked += () => ButtonModePressed(buttonColor, Mode.Color);
        
            var buttonBlockPicker = root.Q<Button>("blockPicker");
            var buttonColorPicker = root.Q<Button>("colorPicker");
            var buttonFile = root.Q<Button>("buttonFile");
            
            var blockWindow = root.Q("LayoutBlocks");
            var colorWindow = root.Q("LayoutColors");
            var fileWindow = root.Q("LayoutFiles");
            buttonBlockPicker.clicked += () => SwitchDisplayWindow(blockWindow);
            buttonColorPicker.clicked += () => SwitchDisplayWindow(colorWindow);
            buttonFile.clicked += () => SwitchDisplayWindow(fileWindow);

            buttonUndo = root.Q<Button>("buttonUndo");
            buttonRedo = root.Q<Button>("buttonRedo");
            buttonUndo.clicked += () => ButtonUndoPressed(buttonUndo);
            buttonRedo.clicked += () => ButtonRedoPressed(buttonRedo);

            root.Q<Button>("buttonSave").clicked += () => manager.SaveProject();
            root.Q<Button>("buttonLoad").clicked += () => manager.LoadProject();
            root.Q<Button>("buttonGenerate").clicked += () => manager.GenerateInstructionsFromProject();

            blockMenu = new UIBlockMenu(this, buttonBlockPicker, blockWindow);
            colorMenu = new UIColorMenu(this, buttonColorPicker);
            menuManager = new UIMenuManager(this);
            blockMenu.colorMenu = colorMenu;
            RegisterMouseCallbacks();
        }

        public UIBlockMenu GetBlockMenu() {
            return blockMenu;
        }

        public UIColorMenu GetColorMenu() {
            return colorMenu;
        }

        public UIMenuManager GetMenu() {
            return menuManager;
        }

        public BlockPlacer GetPlacer() {
            return placer;
        }

        public ProjectManager GetManager() {
            return manager;
        }

        // Změní aktuální mód podle předaného argumentu.
        public void SwitchMode(Mode mode) {
            switch (mode) {
                case Mode.Builder:
                    ButtonModePressed(root.Q<Button>("buttonBuilder"),Mode.Builder);
                    break;
                case Mode.Remover:
                    ButtonModePressed(root.Q<Button>("buttonRemover"),Mode.Remover);
                    break;
                case Mode.Color:
                    ButtonModePressed(root.Q<Button>("buttonColor"),Mode.Color);
                    break;
            }
        }

        // Pokud vybereme jeden z nástrojů, jeho příslušné tlačítko se označí šedou barvou a předešlé tlačítko
        // se vrátí do púvodního stavu.
        private void ButtonModePressed(Button button, Mode mode) {
            if (button == pressedButton) return;
            pressedButton.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);
            placer.ChangeMode(mode);
            button.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0.5f);
            pressedButton = button;

            if (mode == Mode.Color) {
                colorMenu.EnableColors();
            }
            else {
                blockMenu.ChangeColorButtonState(placer.blockIndex);
                if (activeWindow == root.Q("LayoutColors")) {
                    SwitchDisplayWindow(activeWindow);
                }
            }
        }

        // Při kliknutí na tlačítko výběru bloku nebo barvy se zobrazí příslušné okno. Okno pro výběr barvy a
        // okno pro bloky nemohou být současně zobrazeny, takže se jedno z nich opět skryje.
        private void SwitchDisplayWindow(VisualElement window) {
            if (activeWindow != window) {
                activeWindow?.Q("Container").RemoveFromClassList("card-active");
                activeWindow = window;
                activeWindow.Q("Container").AddToClassList("card-active");
            }
            else if (activeWindow == window) {
                activeWindow.Q("Container").RemoveFromClassList("card-active");
                activeWindow = null;
            }
        }

        // Zruší předešlou operaci na desce.
        private void ButtonUndoPressed(Button button) {
            if (button.pickingMode != PickingMode.Ignore) {
                reversibility.Undo();
            }
        }
        
        // Znovu udělá zrušenou operaci na desce.
        private void ButtonRedoPressed(Button button) {
            if (button.pickingMode != PickingMode.Ignore) {
                reversibility.Redo();
            }
        }

        // Zruší nebo povolí možnost kliknout na tlačítko Undo/Redo.
        public void ToggleReverseButton(bool undo) {
            Button button = undo ? buttonUndo : buttonRedo;
            // Když vytváříme nový projekt, tlačítko se nepřepne protože editor není aktivní a tlačítka jsou ignore vždy.
            if (button.pickingMode == PickingMode.Ignore) {
                button.pickingMode = PickingMode.Position;
                button.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);
            }
            else {
                button.pickingMode = PickingMode.Ignore;
                button.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0.5f);
            }
        }

        // V některých případech chceme ukázat uživateli informaci o tom, že se nějaká operace (ne)podařila.
        // Zpráva se tedy zobrazí uživateli a po krátké době zmizí.
        public void ShowMessage(string text, bool error) {
            toast.style.transitionDuration = new StyleList<TimeValue>(0);
            Label message = toast.Q<Label>("MessageText");
            message.text = text;
            message.style.color = error ? new Color(1, 0.5f, 0.5f) : new Color(1, 1, 1);
            toast.style.opacity = 1;
            FadeOutMessage();
        }

        private async void FadeOutMessage() {
            await Task.Delay(1200);
            toast.style.transitionDuration = new List<TimeValue>() { new (1f, TimeUnit.Second) };
            toast.style.opacity = 0;
        }

        // Zobrazí varování o překročení limity stavění. Stav varování kontrolujeme přes proměnnou
        // jelikož kdybychom kontrolovly stav přímo, hodnota by mohla být špatná, jelikož se skutečně změní
        // až po nějakém spoždění.
        public void ToggleWarning(bool show) {
            if (show && warningDisplayed == false) {
                warning.style.display = DisplayStyle.Flex;
                warningDisplayed = true;
            } 
            else if (!show && warningDisplayed) {
                warning.style.display = DisplayStyle.None;
                warningDisplayed = false;
            }
        }


        // Pokud máme myš na UI, nebudeme moct pokládat bloky skrz UI.
        private void MouseEnterCallback(MouseEnterEvent evt) {
            if (!menuManager.IsActive()) {
                placer.FocusOnPlacer(false);   
            }
        }

        // Pokud nemáme myš na UI, budeme moct pokládat bloky.
        private void MouseLeaveCallback(MouseLeaveEvent evt) {
            if (!menuManager.IsActive()) {
                placer.FocusOnPlacer(true);
            }
        }

        // Každé menu bude odebírat událost ohledně informace zda-li je myš nad daným menu.
        private void RegisterMouseCallbacks() {
            foreach (var menu in menus) {
                menu.RegisterCallback<MouseEnterEvent>(MouseEnterCallback);
                menu.RegisterCallback<MouseLeaveEvent>(MouseLeaveCallback);
            }
        }
    }
}
