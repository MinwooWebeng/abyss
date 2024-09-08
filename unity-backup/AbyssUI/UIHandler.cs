using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Executor executor;

    private VisualElement root;
    private TextField addressBar;
    private Label localAddrLabel;
    private TextField sub_addressBar;
    private Label extraLabel; //TODO
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
        localAddrLabel = root.Query<VisualElement>("background").First().Query<Label>("info").First();
        if (localAddrLabel == null)
        {
            Debug.LogError("addr label not found!");
        }

        // Define the log file path in the same directory as the executable
        executor.SetLocalAddrCallback = (addr) => { localAddrLabel.text = addr; };
        sub_addressBar = root.Query<VisualElement>("background").First().Query<TextField>("sub-address-bar").First();
        sub_addressBar.RegisterCallback<KeyDownEvent>((x) =>
        {
            if (x.keyCode == KeyCode.Return)
            {
                SubAddressBarSubmit(sub_addressBar.value);
            }
        });

        extraLabel = root.Query<VisualElement>("background").First().Query<Label>("info-more").First();
        executor.SetAdditionalInfoCallback = (info) => { extraLabel.text = info; };
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
    void SubAddressBarSubmit(string address)
    {
        if (address.StartsWith("connect "))
        {
            var conn_addr = address["connect ".Length..];
            executor.ConnectPeer(conn_addr);
            return;
        }
        executor.LoadContent(address);
    }
}
