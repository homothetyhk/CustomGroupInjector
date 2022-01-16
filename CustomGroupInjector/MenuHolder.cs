using MenuChanger;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using MenuChanger.Extensions;

namespace CustomGroupInjector
{
    public class MenuHolder
    {
        public static MenuHolder Instance { get; private set; }

        public MenuPage MainPage;
        public MenuPage SettingsPage;
        public SmallButton JumpButton;
        public MultiGridItemPanel Panel;
        public IMenuElement[] PackToggles;
        public OrderedItemViewer SettingsViewer;

        public static void OnExitMenu()
        {
            Instance = null;
        }

        public static void ConstructMenu(MenuPage connectionsPage)
        {
            Instance ??= new();
            Instance.OnMenuConstruction(connectionsPage);
        }

        public void OnMenuConstruction(MenuPage connectionsPage)
        {
            MainPage = new("Custom Group Injector Main Menu", connectionsPage);
            SettingsPage = new("Custom Group Injector Settings Page", MainPage);
            JumpButton = new(connectionsPage, "Custom Group Injection");
            JumpButton.AddHideAndShowEvent(MainPage);
            List<Subpage> subpages = new();
            List<SmallButton> pageButtons = new();
            foreach (CustomGroupPack pack in CustomGroupInjectorMod.Packs) CreatePackSubpage(pack, subpages, pageButtons);
            Panel = new(MainPage, 5, 3, 60f, 650f, new(0, 300), pageButtons.ToArray());
            SettingsViewer = new(SettingsPage, subpages.ToArray());
        }

        public void CreatePackSubpage(CustomGroupPack pack, List<Subpage> subpages, List<SmallButton> pageButtons)
        {
            Subpage page = new(SettingsPage, pack.Name);
            List<IMenuElement> pageElements = new();
            
            ToggleButton randomize = new(SettingsPage, "Randomize On Start");
            randomize.ValueChanged += b =>
            {
                if (b) CustomGroupInjectorMod.GS.RandomizedPacks.Add(pack.Name);
                else CustomGroupInjectorMod.GS.RandomizedPacks.Remove(pack.Name);
            };
            randomize.SetValue(CustomGroupInjectorMod.GS.RandomizedPacks.Contains(pack.Name));
            pageElements.Add(randomize);

            SmallButton reset = new(SettingsPage, "Reset");
            pageElements.Add(reset);

            List<NumericEntryField<int>> entryFields = pack.GroupNames.Select(g => CreateGroupField(g)).ToList();
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
            pageButtons.Add(jump);
        }        

        public NumericEntryField<int> CreateGroupField(string groupName)
        {
            NumericEntryField<int> entryField = new(SettingsPage, groupName);
            entryField.ValueChanged += (i) => CustomGroupInjectorMod.GS.GroupSettings[groupName] = i;
            entryField.SetClamp(-1, 99);
            if (!CustomGroupInjectorMod.GS.GroupSettings.TryGetValue(groupName, out int value))
            {
                CustomGroupInjectorMod.GS.GroupSettings[groupName] = value = -1;
            }
            entryField.SetValue(value);
            return entryField;
        }

        public static bool TryGetMenuButton(MenuPage connectionsPage, out SmallButton button)
        {
            return Instance.TryGetJumpButton(connectionsPage, out button);
        }

        public bool TryGetJumpButton(MenuPage connectionsPage, out SmallButton button)
        {
            button = JumpButton;
            return true;
        }
    }
}
