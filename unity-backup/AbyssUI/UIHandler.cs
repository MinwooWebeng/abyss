using UnityEngine;
using UnityEngine.UIElements;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Executor executor;

    private VisualElement root;
    private TextField addressBar;
    void Awake()
    {
        root = uiDocument.rootVisualElement;
        addressBar = root.Query<VisualElement>("background").First().Query<TextField>("address-bar").First();
        addressBar.RegisterCallback<KeyDownEvent>((x) =>
        {
            if (x.keyCode == KeyCode.Return)
            {
                AddressBarSubmit(addressBar.value);
            }
        });
        Deactivate();
    }
    public void Activate()
    {
        root.visible = true;
        addressBar.focusable = true;
    }
    public void Deactivate()
    {
        root.visible = false;
        addressBar.focusable = false;
    }
    void AddressBarSubmit(string address)
    {
        executor.MoveWorld(address);
    }
}
