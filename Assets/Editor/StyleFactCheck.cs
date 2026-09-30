using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class StyleFactCheck
{
    static Sprite rounded;
    static TextMeshProUGUI template;

    [MenuItem("Tools/Food for Thought/Style Fact Check")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorSceneManager.SaveOpenScenes();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/2 Home Page.unity");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var options=all.First(t=>t.name=="FactCheckOptions");
        var detail=all.First(t=>t.name=="FactCheckPanel");
        var board=all.First(t=>t.name=="AnnouncementBoardPanel");
        template=all.Select(t=>t.GetComponent<TextMeshProUGUI>()).First(t=>t!=null && t.text.Trim()=="Daily Post");
        rounded=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        foreach(var panel in new[]{options,detail,board})
        {
            Place(panel,0,0,1,1);
            Paint(panel,GamePalette.Background);
            var decoration=panel.Find("Image"); if(decoration!=null) decoration.gameObject.SetActive(false);
            foreach(var text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            { text.color=GamePalette.Text; text.enableVertexGradient=false; text.raycastTarget=false; }
            var back=panel.Find("Exit Button");
            Place(back,.07f,.90f,.15f,.96f);
            back.GetComponent<Image>().color=GamePalette.Text;
        }
        Label(options,"Text (TMP)","How will you respond?",.08f,.76f,.92f,.87f,42,true);
        Label(options,"ResponseIntro","A fact check has challenged your post. Choose your next step.",.08f,.64f,.92f,.75f,28,false);
        Choice(options.Find("1"),"Ignore","Leave the claim unanswered",.08f,.40f,.48f,.59f);
        Choice(options.Find("2"),"Apologize","Publish a correction",.52f,.40f,.92f,.59f);
        Choice(options.Find("3"),"Hire people","Hire help to correct the post",.08f,.17f,.48f,.36f);
        Choice(options.Find("4"),"Defend","Stand by your claim",.52f,.17f,.92f,.36f);
        // Remove the original duplicate Ignore label, which was stacked on the first.
        var duplicate=options.Find("1/Text (TMP) (1)");if(duplicate!=null)duplicate.gameObject.SetActive(false);
        var content=detail.Find("FactCheck");Place(content,.08f,.09f,.92f,.85f);Paint(content,GamePalette.Secondary);
        Label(content,"Text (TMP) (1)","You've been fact-checked",.07f,.79f,.93f,.94f,40,true);
        Label(content,"Text (TMP)","Publish a post to begin.",.07f,.17f,.93f,.77f,26,false);
        // Keep the existing reach display; only its presentation changes.
        Place(content.Find("Image"),.08f,.19f,.17f,.27f);
        Place(content.Find("Text (TMP) (3)"),.20f,.19f,.70f,.27f);
        var reach=content.Find("Text (TMP) (3)").GetComponent<TextMeshProUGUI>();reach.fontSize=26;reach.enableAutoSizing=false;
        Button(content.Find("Button"),"Choose a response",.07f,.04f,.93f,.14f);
        Label(board,"BoardTitle","Fact Check Alert",.08f,.78f,.92f,.88f,42,true);
        var post=board.Find("Post 1");Place(post,.08f,.30f,.92f,.74f);Paint(post,GamePalette.Secondary);
        Place(post.Find("Image"),.06f,.32f,.94f,.95f);
        Label(post,"Text (TMP)","No fact checks yet.",.07f,.08f,.93f,.92f,26,false);
        Button(board.Find("Post 2"),"View fact check",.08f,.15f,.92f,.25f);
        // Decorative button art would cover the new consistent surface.
        var art=board.Find("Post 2/Image");if(art!=null)art.gameObject.SetActive(false);
        foreach(var entry in new[]{all.First(t=>t.name=="AnnouncementPreviewPanel"),
            board.Find("Post 1/Image"),board.Find("Post 2"),content.Find("Button")})
            if(entry.GetComponent<FactCheckEntryState>()==null)
                entry.gameObject.AddComponent<FactCheckEntryState>();
        BuildResponseFeedback(options);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("Fact Check styled; existing button actions preserved.");
    }
    static void BuildResponseFeedback(Transform options)
    {
        var controller=options.GetComponent<FactCheckResponseFeedback>();
        bool firstBuild=controller==null;
        if(firstBuild)controller=options.gameObject.AddComponent<FactCheckResponseFeedback>();
        var panel=options.Find("ResponseFeedback");
        if(panel==null)
        {
            panel=new GameObject("ResponseFeedback",typeof(RectTransform),typeof(Image)).transform;
            panel.SetParent(options,false);
        }
        Place(panel,0,0,1,1);Paint(panel,GamePalette.Background);
        panel.GetComponent<Image>().raycastTarget=true;
        controller.feedbackPanel=panel.gameObject;
        Label(panel,"Eyebrow","YOUR RESPONSE STRATEGY",.08f,.90f,.92f,.95f,25,true);
        controller.heading=Label(panel,"Heading","Own the mistake",.08f,.78f,.92f,.87f,42,true);
        controller.explanation=Label(panel,"Explanation","",.08f,.43f,.92f,.76f,24,false);
        var tutor=panel.Find("CatTutor");
        if(tutor==null)
        {
            tutor=new GameObject("CatTutor",typeof(RectTransform),typeof(Image)).transform;
            tutor.SetParent(panel,false);
        }
        Place(tutor,.08f,.24f,.92f,.41f);Paint(tutor,GamePalette.Secondary);
        var portrait=tutor.Find("Expression");
        if(portrait==null)
        {
            portrait=new GameObject("Expression",typeof(RectTransform),typeof(Image)).transform;
            portrait.SetParent(tutor,false);
        }
        Place(portrait,.03f,.12f,.28f,.88f);
        controller.tutorImage=portrait.GetComponent<Image>();
        controller.tutorImage.color=Color.white;
        controller.tutorImage.preserveAspect=true;
        controller.tutorImage.raycastTarget=false;
        controller.apologyCat=CatSprite("catface");
        controller.hiringCat=CatSprite("catfacesideeye2");
        controller.defenseCat=CatSprite("catfacesideeye3");
        controller.tutorImage.sprite=controller.apologyCat;
        controller.tutorMessage=Label(tutor,"Advice","",.33f,.09f,.95f,.91f,22,false);
        var next=FeedbackButton(panel,"Continue","Continue",.08f,.14f,.92f,.23f);
        controller.continueLabel=next.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
        next.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
        UnityEventTools.AddPersistentListener(next.onClick,controller.Continue);
        var back=FeedbackButton(panel,"Back","Choose another strategy",.08f,.04f,.92f,.12f);
        Paint(back.transform,GamePalette.Secondary);
        back.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
        UnityEventTools.AddPersistentListener(back.onClick,controller.Back);
        controller.backButton=back.gameObject;
        var buttons=new[]{"1","2","3","4"};
        UnityEngine.Events.UnityAction[] actions={controller.Ignore,controller.Apologize,controller.Hire,controller.Defend};
        for(int i=0;i<buttons.Length;i++)
        {
            var button=options.Find(buttons[i]).GetComponent<UnityEngine.UI.Button>();
            button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick,actions[i]);
        }
        // All committed responses show their outcome before returning home.
        var sceneObjects=options.gameObject.scene.GetRootGameObjects()
            .SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        controller.returnToMenu=new UnityEngine.Events.UnityEvent();
        foreach(var name in new[]{"FactCheckPanel","AnnouncementBoardPanel"})
            UnityEventTools.AddBoolPersistentListener(controller.returnToMenu,
                sceneObjects.First(t=>t.name==name).gameObject.SetActive,false);
        UnityEventTools.AddBoolPersistentListener(controller.returnToMenu,
            sceneObjects.First(t=>t.name=="MainMenuPanel").gameObject.SetActive,true);
        UnityEventTools.AddBoolPersistentListener(controller.returnToMenu,options.gameObject.SetActive,false);
        panel.gameObject.SetActive(false);
    }
    static Sprite CatSprite(string name)
    {
        string path="Assets/Food for Thought Images/Cat/"+name+".png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer.textureType!=TextureImporterType.Sprite)
        {
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static UnityEngine.UI.Button FeedbackButton(Transform parent,string name,string title,float x,float y,float r,float top)
    {
        var t=parent.Find(name);
        if(t==null)
        {
            t=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(UnityEngine.UI.Button)).transform;
            t.SetParent(parent,false);
        }
        Button(t,title,x,y,r,top);
        return t.GetComponent<UnityEngine.UI.Button>();
    }
    static void Place(Transform t,float x,float y,float r,float top)
    {
        var rt=(RectTransform)t;rt.localScale=Vector3.one;
        rt.anchorMin=new Vector2(x,y);rt.anchorMax=new Vector2(r,top);
        rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;
    }
    static void Paint(Transform t,Color color)
    {
        var image=t.GetComponent<Image>();
        if(image==null)image=t.gameObject.AddComponent<Image>();
        image.enabled=true;
        image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color;image.material=null;
        var button=t.GetComponent<UnityEngine.UI.Button>();
        image.raycastTarget=button!=null;
        if(button!=null)
        {
            button.targetGraphic=image;
            button.transition=Selectable.Transition.ColorTint;
            var colors=ColorBlock.defaultColorBlock;
            colors.highlightedColor=new Color(1f,.97f,.88f);
            colors.pressedColor=new Color(.88f,.84f,.75f);
            button.colors=colors;
        }
    }
    static TextMeshProUGUI Label(Transform parent,string name,string value,float x,float y,float r,float top,float size,bool bold)
    {
        var t=parent.Find(name);
        var label=t!=null?t.GetComponent<TextMeshProUGUI>():Object.Instantiate(template,parent);
        label.name=name;label.gameObject.SetActive(true);label.text=value;
        label.font=template.font;label.fontSharedMaterial=template.fontSharedMaterial;
        label.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;
        label.color=GamePalette.Text;label.enableVertexGradient=false;label.raycastTarget=false;
        label.margin=Vector4.zero;
        label.enableAutoSizing=true;label.fontSizeMin=size-4;label.fontSizeMax=size;
        label.alignment=TextAlignmentOptions.TopLeft;label.textWrappingMode=TextWrappingModes.Normal;
        Place(label.transform,x,y,r,top);return label;
    }
    static void Choice(Transform t,string title,string description,float x,float y,float r,float top)
    {
        Place(t,x,y,r,top);Paint(t,GamePalette.Secondary);
        Label(t,"Text (TMP)",title,.08f,.49f,.92f,.87f,34,true);
        Label(t,"ChoiceDescription",description,.08f,.10f,.92f,.45f,24,false);
        // A narrow yellow accent ties all choices to the game's action color.
        var accent=t.Find("ChoiceAccent");
        if(accent==null){accent=new GameObject("ChoiceAccent",typeof(RectTransform),typeof(Image)).transform;accent.SetParent(t,false);}
        Place(accent,0,0,.025f,1);Paint(accent,GamePalette.Primary);accent.GetComponent<Image>().raycastTarget=false;
    }
    static void Button(Transform t,string title,float x,float y,float r,float top)
    {
        Place(t,x,y,r,top);Paint(t,GamePalette.Primary);
        var label=Label(t,"Text (TMP)",title,.04f,.08f,.96f,.92f,32,true);
        label.alignment=TextAlignmentOptions.Center;label.color=GamePalette.OnPrimary;
    }
}
