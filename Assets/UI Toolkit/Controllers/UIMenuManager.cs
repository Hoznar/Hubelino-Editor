using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI_Toolkit.Controllers {
    public class UIMenuManager {
        private List<Button> buttons;
        private bool menuActive;
        private bool bootUp;

        private readonly Label labelLength;
        private readonly Label labelWidth;
        
        private readonly TextField setterName;
        private readonly SliderInt setterLength;
        private readonly SliderInt setterWidth;
        
        private readonly VisualElement root;
        private readonly VisualElement settingsWindow;
        private readonly VisualElement projectWindow;
        private readonly BlockPlacer placer;
        private readonly ProjectManager manager;

        public UIMenuManager(UIEditorManager ui) {
            placer = ui.GetPlacer();
            manager = ui.GetManager();
            root = ui.GetComponent<UIDocument>().rootVisualElement;
            projectWindow = root.Q("LayoutMenu");
            settingsWindow = root.Q("LayoutSettings");

            root.Q<Button>("buttonNew").clicked += OpenProjectMenu;
            root.Q<Button>("buttonCreateProject").clicked += OnCreateProjectClick;
            root.Q<Button>("buttonLoadProject").clicked += OnLoadProjectClick;
            root.Q<Button>("buttonClose").clicked += CloseProjectMenu;
            
            setterName = root.Q<TextField>("setterName");
            setterLength = root.Q<SliderInt>("setterLength");
            setterWidth = root.Q<SliderInt>("setterWidth");

            labelLength = root.Q<Label>("labelLength");
            labelWidth = root.Q<Label>("labelWidth");

            setterLength.RegisterValueChangedCallback(UpdateLengthField);
            setterWidth.RegisterValueChangedCallback(UpdateWidthField);

            Screen.SetResolution(Screen.currentResolution.width,Screen.currentResolution.height,FullScreenMode.MaximizedWindow);
            var resolutionButton = root.Q<DropdownField>("ResolutionPicker");
            resolutionButton.index = 0;
            resolutionButton.RegisterValueChangedCallback(ChangeResolution);
            
            var fullscreenButton = root.Q<Toggle>("FullscreenToggle");
            fullscreenButton.RegisterValueChangedCallback(e => Screen.fullScreen = !Screen.fullScreen);
            
            root.Q<Button>("QuitButton").clicked += Application.Quit;
            OpenProjectMenu();
            bootUp = true;
        }

        public bool IsActive() {
            return menuActive;
        }
        
        // Zobrazí uživateli menu vytvoření nového projektu. V případě prvního načtení programu je
        // navíc možnost načtení projektu a není možné okno schovat.
        private void OpenProjectMenu() {
            if (bootUp) {
                root.Q<VisualElement>("buttonClose").style.display = DisplayStyle.Flex;
                root.Q<Button>("buttonLoadProject").style.display = DisplayStyle.None;
                bootUp = false;
            }
            setterName.value = "My custom build";
            setterLength.value = setterLength.lowValue;
            setterWidth.value = setterWidth.lowValue;
            labelLength.text = "12";
            labelWidth.text = "12";
            projectWindow.style.display = DisplayStyle.Flex;
            ToggleEditor(false);
        }

        // Zavře okno vytvoření nového projektu.
        private void CloseProjectMenu() {
            projectWindow.style.display = DisplayStyle.None;
            ToggleEditor(true);
        }

        // Zpracuje nastavené parametry a pošle je ProjectManageru pro vytvoření nového projektu.
        // Pak schová okno.
        private void OnCreateProjectClick() {
            CloseProjectMenu();
            manager.CreateNewProject(setterName.value,setterLength.value * 2,setterWidth.value * 2);
        }

        // Požádá ProjectManager o načtení souboru a schová okno.
        private void OnLoadProjectClick() {
            if (manager.LoadProject()) {
                CloseProjectMenu();
            }
        }

        // Zobrazí/schová menu pro nastavení a odchodu z aplikace. Také pozastaví veškerou jinou funkcionalitu.
        public void OpenSettings() {
            if (projectWindow.resolvedStyle.display == DisplayStyle.Flex) return;
            if (settingsWindow.resolvedStyle.display == DisplayStyle.None) {
                settingsWindow.style.display = DisplayStyle.Flex;
                ToggleEditor(false);
            }
            else {
                settingsWindow.style.display = DisplayStyle.None;
                ToggleEditor(true);
            }
        }
        
        // Určí zda-li budeme moct ovládat prvky editoru.
        private void ToggleEditor(bool value) {
            placer.FocusOnPlacer(value);
            ToggleButtons(value);
            menuActive = !menuActive;
        }

        // Zruší/povolí funkcionalitu všech tlačítek v našem UI.
        private void ToggleButtons(bool enable) {
            if (!enable) {
                buttons = new List<Button>();
                buttons.AddRange(root.Query<Button>(className: "btn").Where(b => b.pickingMode == PickingMode.Position).ToList());
            }
            foreach (var button in buttons) {
                button.pickingMode = enable ? PickingMode.Position : PickingMode.Ignore;
            }
        }
        
        // Nastaví ukazatel hodnoty slideru na dvojnásobek.
        private void UpdateLengthField(ChangeEvent<int> evt) {
            labelLength.text = (evt.newValue * 2).ToString();
        }
        
        private void UpdateWidthField(ChangeEvent<int> evt) {
            labelWidth.text = (evt.newValue * 2).ToString();
        }

        // Změní rozlišení obrazovky podle výběru.
        private static void ChangeResolution(ChangeEvent<string> evt) {
            string resolution = evt.newValue;
            int index = resolution.IndexOf('x');
            float width = float.Parse(resolution[..index]);
            float height = float.Parse(resolution.Substring(index + 1, resolution.Length - index - 1));
            
            Screen.SetResolution((int)width,(int)height,Screen.fullScreen);
        }
    }
}
