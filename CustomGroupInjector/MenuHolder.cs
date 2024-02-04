using MenuChanger;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using MenuChanger.Extensions;

namespace CustomGroupInjector
{
    public class MenuHolder
    {
        public static MenuHolder Instance { get; private set; }

        public MenuPage ConnectionsPage;
        public MenuPage MainPage;
        public MenuPage SettingsPage;
        public SmallButton JumpButton;
        public MultiGridItemPanel Panel;
        public OrderedItemViewer SettingsViewer;
        public Dictionary<string, ToggleButton> PackRandoToggleLookup = [];
        public Dictionary<string, NumericEntryField<int>> GroupFieldLookup = [];
        public SmallButton? RestoreLocalPacks;

        public static void OnExitMenu()
        {
            Instance = null;
        }

        public static void ConstructMenu(MenuPage connectionsPage)
        {
            Instance ??= new() {  };
            Instance.OnMenuConstructionFirstTime(connectionsPage);
            Instance.OnMenuConstruction();
        }

        public void ReconstructMenu()
        {
            JumpButton.ClearOnClick();
            RestoreLocalPacks = null;
            UnityEngine.Object.Destroy(MainPage.self);
            UnityEngine.Object.Destroy(SettingsPage.self);
            OnMenuConstruction();
        }

        public void OnMenuConstructionFirstTime(MenuPage connectionsPage)
        {
            ConnectionsPage = connectionsPage;
            JumpButton = new(ConnectionsPage, "Custom Group Injection");
            connectionsPage.BeforeShow += () => JumpButton.Text.color = CustomGroupInjectorMod.GS.IsActive() ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
        }

        public void OnMenuConstruction()
        {
            MainPage = new("Custom Group Injector Main Menu", ConnectionsPage);
            SettingsPage = new("Custom Group Injector Settings Page", MainPage);
            JumpButton.AddHideAndShowEvent(MainPage);
            PackRandoToggleLookup.Clear();
            GroupFieldLookup.Clear();
            List<Subpage> subpages = [];
            List<SmallButton> pageButtons = [];
            foreach (CustomGroupPack pack in CustomGroupInjectorMod.Packs) CreatePackSubpage(pack, subpages, pageButtons);
            Panel = new(MainPage, 5, 3, 60f, 650f, new(0, 300), pageButtons.ToArray());
            SettingsViewer = new(SettingsPage, subpages.ToArray());
        }

        public void CreateRestoreLocalPacksButton()
        {
            RestoreLocalPacks = new SmallButton(MainPage, "Restore Local Packs");
            RestoreLocalPacks.OnClick += () =>
            {
                MainPage.Hide();
                CustomGroupInjectorMod.LoadFiles();
                ReconstructMenu();
                MainPage.Show();
            };
            RestoreLocalPacks.MoveTo(new(0f, -300f));
            RestoreLocalPacks.SymSetNeighbor(Neighbor.Up, Panel);
            RestoreLocalPacks.SymSetNeighbor(Neighbor.Down, MainPage.backButton);
        }

        public void ToggleAllOff()
        {
            foreach (ToggleButton b in PackRandoToggleLookup.Values) if (b.Value) b.SetValue(false);
            foreach (NumericEntryField<int> e in GroupFieldLookup.Values) if (e.Value >= 0) e.SetValue(-1);
        }

        public void CreatePackSubpage(CustomGroupPack pack, List<Subpage> subpages, List<SmallButton> pageButtons)
        {
            Subpage page = new(SettingsPage, pack.Name);
            List<IMenuElement> pageElements = [];
            
            ToggleButton randomize = new(SettingsPage, "Randomize On Start");
            randomize.ValueChanged += b =>
            {
                if (b) CustomGroupInjectorMod.GS.SetPackRandomization(pack.Name, true);
                else CustomGroupInjectorMod.GS.SetPackRandomization(pack.Name, false);
            };
            randomize.SetValue(CustomGroupInjectorMod.GS.IsPackRandomized(pack.Name));
            pageElements.Add(randomize);
            PackRandoToggleLookup.Add(pack.Name, randomize);

            SmallButton reset = new(SettingsPage, "Reset");
            pageElements.Add(reset);

            List<NumericEntryField<int>> entryFields = pack.GetGroupNames().Select(g => CreateGroupField(g)).ToList();
            GridItemPanel grid = new(SettingsPage, new(0f, 0f), 2, 75, 800f, false, entryFields.ToArray());
            pageElements.Add(grid);

            reset.OnClick += () =>
            {
                randomize.SetValue(false);
                foreach (var e in entryFields) e.SetValue(-1);
            };

            VerticalItemPanel panel = new(SettingsPage, new(0, 325f), 75f, false, pageElements.ToArray());
            page.Add(panel);
            subpages.Add(page);

            SmallButton jump = new(MainPage, pack.Name);
            jump.OnClick += () =>
            {
                MainPage.TransitionTo(SettingsPage);
                jump.Button.ForceDeselect();
                SettingsViewer.JumpTo(page);
                SettingsPage.nav.SelectDefault();
            };
            MainPage.BeforeShow += () => jump.Text.color = CustomGroupInjectorMod.GS.IsPackEnabled(pack) ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
            pageButtons.Add(jump);
        }        

        public NumericEntryField<int> CreateGroupField(string groupName)
        {
            NumericEntryField<int> entryField = new(SettingsPage, groupName);
            entryField.ValueChanged += (i) => CustomGroupInjectorMod.GS.SetGroupSetting(groupName, i);
            entryField.SetClamp(-1, 99);
            entryField.SetValue(CustomGroupInjectorMod.GS.GetGroupSetting(groupName));
            GroupFieldLookup.Add(groupName, entryField);
            return entryField;
        }

        public static bool TryGetMenuButton(MenuPage connectionsPage, out SmallButton button)
        {
            button = Instance.JumpButton;
            return true;
        }
    }
}
