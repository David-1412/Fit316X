
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CollectionDetails;
using static DescriptionElement;

public class CollectionDetailsManager : MonoBehaviour
{

    public GameObject SimpleDescriptionPanel;
    public GameObject AdvancedDescriptionPanel;

    public GameObject SubtitlePrefab;
    public GameObject BodyPrefab;

    [Serializable]
    protected struct Panel {
        public TextMeshProUGUI NameTextBox;
        public TextMeshProUGUI DescriptionTextBox;
        public Image ImageBox;
        public Transform LoreBox;
    }

    [SerializeField]
    private Panel Simple;
    [SerializeField]
    private Panel Advanced;

    private readonly static List<GameObject> OFFBodyPool = new();
    private readonly static List<GameObject> OFFSubTitlePool = new();
    private readonly static List<GameObject> BodyPool = new();
    private readonly static List<GameObject> SubTitlePool = new();

    private LoreCollection defaultLoreSet = new LoreCollection() { Subtitle = "???", Bodies = new() { "Keep exploring to unlock this entry" } };
    
    public UnityEngine.Object CurrentDisplay;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (FindObjectsByType<CollectionDetailsManager>().Length > 1)
        {
            Debug.LogError("More Than One Collection Details Manager Already Active. Deactivating this element", gameObject);
            gameObject.SetActive(false);
        }

        // Check instantiation
        Debug.Assert(SimpleDescriptionPanel != null);
        Debug.Assert(AdvancedDescriptionPanel != null);


        // Collect relevant children
        Simple = ToPanel(SimpleDescriptionPanel.GetComponentsInChildren<DescriptionElement>(true), false);
        Advanced = ToPanel(AdvancedDescriptionPanel.GetComponentsInChildren<DescriptionElement>(true), true);

        SimpleDescriptionPanel.SetActive(false);
        AdvancedDescriptionPanel.SetActive(false);

    }

    private Panel ToPanel(DescriptionElement[] source, bool advanced)
    {
        Panel panel = new Panel();


        foreach (DescriptionElement element in source)
        {
            switch (element.type)
            {
                case DisplayType.Title:
                    panel.NameTextBox = element.gameObject.GetComponent<TextMeshProUGUI>();
                    break;
                case DisplayType.Description:
                    panel.DescriptionTextBox = element.gameObject.GetComponent<TextMeshProUGUI>();
                    break;
                case DisplayType.CollectableImage:
                    panel.ImageBox = element.gameObject.GetComponent<Image>();
                    break;
                case DisplayType.LoreContainer:
                    panel.LoreBox = element.gameObject.GetComponent<Transform>();
                    break;
                default:
                    Debug.Log(element.type.ToString());
                    break;

            }
        }

        Debug.LogAssertion(panel.NameTextBox != null);
        Debug.LogAssertion(panel.DescriptionTextBox != null);
        Debug.LogAssertion(panel.ImageBox != null);

        if (advanced)
        {
            Debug.LogAssertion(panel.LoreBox != null);
        }

        return panel;
    }


    public void Display(CollectionDetails details)
    {
        Panel CurrentPanel; 

        if (details.Lore.Count <= 0) { 
            CurrentPanel = Simple;
            SimpleDescriptionPanel.SetActive(true);
        }
        else { 
            CurrentPanel = Advanced;
            AdvancedDescriptionPanel.SetActive(true);
        }

        CurrentPanel.NameTextBox.text = details.Name;
        CurrentPanel.DescriptionTextBox.text = details.Description;
        CurrentPanel.ImageBox.sprite = details.Image;

        if (details.Lore.Count > 0) { DisplayLoreList(details.Lore); }
    }


    public void DisplayLoreList(List<LoreCollection> Lore)
    {

        CleanLore();

        
        foreach (LoreCollection loreSet in Lore)
        {
            if (loreSet.PrereqMet())
            {
                DisplayLore(loreSet);
            } else
            {
                DisplayLore(defaultLoreSet);
            }
        }
    }


    // ERROR IS IN HERE
    private void DisplayLore(LoreCollection Lore)
    {
     
        GameObject newSubtitle;
        if (OFFSubTitlePool.Count() > 0)
        {
            newSubtitle = OFFSubTitlePool[0];
            newSubtitle.SetActive(true);
            OFFSubTitlePool.RemoveAt(0);
        }
        else
        {
            newSubtitle = Instantiate(SubtitlePrefab);
        }

        
        newSubtitle.GetComponent<TextMeshProUGUI>().text = Lore.Subtitle;
        SubTitlePool.Add(newSubtitle);

        newSubtitle.transform.SetParent(Advanced.LoreBox, false);
        newSubtitle.transform.SetAsLastSibling();

        GameObject NewBody;
        foreach (string body in Lore.Bodies)
        {
            if (OFFBodyPool.Count() > 0)
            {
                NewBody = OFFBodyPool[0];
                NewBody.SetActive(true);
                OFFBodyPool.RemoveAt(0);
            }
            else
            {
                NewBody = Instantiate(BodyPrefab);
            }

            NewBody.GetComponent<TextMeshProUGUI>().text = body;
            BodyPool.Add(NewBody);


            NewBody.transform.SetParent(Advanced.LoreBox, false);
            NewBody.transform.SetAsLastSibling();
        }

    }

    private void CleanLore()
    {
        foreach (GameObject st in SubTitlePool)
        {
            st.SetActive(false);
            OFFSubTitlePool.Add(st);
        }
        SubTitlePool.Clear();


        foreach (GameObject b in BodyPool)
        {
            b.SetActive(false);
            OFFBodyPool.Add(b);
        }
        BodyPool.Clear();
    }


    public void CloseDisplay()
    {
        SimpleDescriptionPanel.SetActive(false);
        AdvancedDescriptionPanel.SetActive(false);
        CurrentDisplay = null;
    }

}
