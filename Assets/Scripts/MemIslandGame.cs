using System;
using System.Collections.Generic;
using UnityEngine;

// MEM ISLAND: self-contained playable level using built-in Unity primitives.
// All models are temporary original low-poly shapes, not copies of meme images.
public class MemIslandGame : MonoBehaviour
{
    class Collectible { public GameObject root; public Vector3 place; public bool taken; }
    class Enemy { public GameObject root; public string name; public float speed, nextHit; public bool defeated; }
    class AirShot { public GameObject root; public Vector3 direction; public float ttl; }

    readonly List<Collectible> collectibles = new List<Collectible>();
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<AirShot> shots = new List<AirShot>();
    readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    CharacterController cc;
    Camera view;
    Light torch;
    GameObject player, cat, cage, boss, pursuer, exitGate;
    Material bossMat;
    AudioSource sounds, chaseMusic;
    AudioClip hitSound, pickSound, bossSound;
    Vector3 vertical = Vector3.zero;
    Vector3 lastCheckpoint = new Vector3(0f, 2f, -5f);
    float yaw = 0, pitch = 0, nextAttack, nextDamage, nextShot, bossClock;
    float bannerUntil, chaseStartTime;
    string banner = "", stage = "СПАВН";
    int hearts = 3, count = 0, bossHP = 10;
    bool gotSlipper, talkedCat, talkedCage, chaseStarted, chaseDone, bossStarted, bossDead, ending;
    bool jumpPressed, attackPressed, talkPressed;
    bool musicChase;
    Vector2 stick = Vector2.zero;
    Vector2 stickCenter = new Vector2(140, 130);
    string subtitle = "Подойди к лавке Котости и нажми «ГОВОРИТЬ»";
    string bossLine = "";
    float bossLineUntil;

    void Start()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.035f, 0.075f, 0.15f);
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.ambientLight = new Color(0.33f, 0.39f, 0.52f);
        SetupScene();
    }

    Material Mat(string name, Color c, bool glows = false)
    {
        Material m;
        if (mats.TryGetValue(name, out m)) return m;
        Shader sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Diffuse");
        m = new Material(sh); m.name = name; m.color = c;
        if (glows && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 0.7f);
        }
        mats.Add(name, m); return m;
    }
    GameObject Cube(string name, Vector3 at, Vector3 scale, Material material, bool solid = true)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name = name; o.transform.position = at; o.transform.localScale = scale;
        o.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) Destroy(o.GetComponent<Collider>());
        return o;
    }
    GameObject Ball(string name, Vector3 at, Vector3 scale, Material material, bool solid = false)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        o.name = name; o.transform.position = at; o.transform.localScale = scale;
        o.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) Destroy(o.GetComponent<Collider>());
        return o;
    }
    void Label(Transform parent, string words, Vector3 offset, Color color, int size = 60)
    {
        var o = new GameObject("Name: " + words); o.transform.SetParent(parent);
        o.transform.localPosition = offset;
        var txt = o.AddComponent<TextMesh>(); txt.text = words; txt.fontSize = size;
        txt.characterSize = 0.08f; txt.anchor = TextAnchor.MiddleCenter;
        txt.alignment = TextAlignment.Center; txt.color = color;
        o.AddComponent<FaceCamera>();
    }
    void Lamp(float x, float z, float h = 4f)
    {
        var post = Mat("post", new Color(0.16f, 0.19f, 0.26f));
        Cube("Фонарный столб", new Vector3(x, h * 0.5f, z), new Vector3(.24f, h, .24f), post);
        Ball("Светильник", new Vector3(x, h, z), Vector3.one * .55f, Mat("lamp", new Color(1f, .8f, .35f), true));
        var l = new GameObject("Тёплый свет").AddComponent<Light>();
        l.type = LightType.Point; l.range = 11f; l.intensity = 2f;
        l.color = new Color(1f, .72f, .39f); l.transform.position = new Vector3(x, h, z);
    }
    void Pine(float x, float z, float h = 4f)
    {
        Cube("Ствол", new Vector3(x, h * .3f, z), new Vector3(.6f, h * .6f, .6f), Mat("wood", new Color(.32f,.18f,.13f)));
        var top = Ball("Крона", new Vector3(x,h*.75f,z), new Vector3(2.7f,h*.68f,2.7f), Mat("pine", new Color(.09f,.31f,.25f)));
        top.transform.eulerAngles = new Vector3(0, x*20f, 0);
    }
    void SetupScene()
    {
        var moon = new GameObject("Лунный свет").AddComponent<Light>();
        moon.type = LightType.Directional; moon.intensity = .52f; moon.color = new Color(.55f,.7f,1f);
        moon.transform.rotation = Quaternion.Euler(45, -40, 0);
        Cube("Тёмное море", new Vector3(0,-9,141), new Vector3(400,1,570), Mat("sea",new Color(.025f,.105f,.18f)));
        var ground = Mat("grass", new Color(.12f,.26f,.23f));
        var road = Mat("road",new Color(.19f,.22f,.3f));
        var ice = Mat("ice",new Color(.44f,.75f,.9f));
        var white = Mat("white", Color.white);
        var dark = Mat("dark",new Color(.08f,.1f,.16f));
        Cube("Стартовый остров",new Vector3(0,-.6f,9),new Vector3(43,1.2f,52),ground);
        for(int k=0;k<13;k++)
        {
            float z = 35+k*4.8f;
            Cube("Асфальт",new Vector3(0,-.4f,z),new Vector3(20,.8f,4.9f),road);
            if(k%2==0) Cube("Разметка",new Vector3(0,.02f,z),new Vector3(.15f,.02f,2f),white,false);
        }
        for (int k=0;k<6;k++)
        {
            float z = 98 + k*9.3f;
            float y = .42f * k;
            Cube("Ледяная тропа",new Vector3(0,y-.35f,z),new Vector3(18,.7f,9.4f),ice);
            Cube("Ледяная глыба",new Vector3(k%2==0?-11:11,y+.7f,z),new Vector3(4,3.3f,5),ice);
        }
        for(int k=0;k<6;k++)
        {
            float z=155+k*9f; float x=(k%3-1)*2.4f;
            Cube("ПАРКУР ПЛАТФОРМА "+(k+1),new Vector3(x,2f,z),new Vector3(8,1.2f,6.2f),Mat("parkour",new Color(.46f,.36f,.73f)));
            Cube("Подсветка края",new Vector3(x,2.65f,z-2.9f),new Vector3(8,.1f,.2f),Mat("edge",new Color(.4f,.85f,1f),true),false);
        }
        Cube("Переход в арену", new Vector3(0,0,209), new Vector3(13,2,11),ground);
        Cube("Арена",new Vector3(0,-.8f,235),new Vector3(45,1.6f,48),Mat("arena",new Color(.2f,.22f,.3f)));
        for(int k=0;k<12;k++)
        {
            float a=k*Mathf.PI*2/12f;
            Cube("Столб арены",new Vector3(Mathf.Sin(a)*21,2,235+Mathf.Cos(a)*22),new Vector3(1.6f,4f,1.6f),dark);
        }
        Cube("Остров финала",new Vector3(0,-.5f,287),new Vector3(36,1,29),ground);
        Cube("Далёкие горы",new Vector3(-28,9,126),new Vector3(25,19,40),ice);
        for(int k=0;k<17;k++)
        {
            float z=-11f+k*3.6f;
            if(k<11) {Pine(-13f-(k%3)*2.3f,z,4+(k%4)); Pine(13+(k%2)*2.1f,z+1,3+(k%3));}
        }
        // ЛЕС СРАЗУ ПОСЛЕ ЛАВКИ И КЛЕТКИ: мемные враги на тропинке.
        // Дорожка посередине (x=-3..3) остаётся свободной для игрока.
        for (int k=0;k<5;k++)
        {
            float z=14f+k*4.4f;
            Pine(-8f-(k%2)*2f,z,4.1f+(k%3));
            Pine(8f+(k%2)*2f,z+1.5f,4.2f+((k+1)%3));
        }
        for(int k=0;k<8;k++) { Lamp(k%2==0?-8.5f:8.5f,35+k*8); }
        Lamp(-7,0); Lamp(8,20); Lamp(3,282);
        SetupShop(); SetupCage(); SetupMemeEnemies(); SetupIceCube(); SetupCollectibles(); SetupBoss();
        exitGate=Cube("Выход после победы",new Vector3(0,2f,257),new Vector3(8,4f,.45f),Mat("exit",new Color(.09f,.75f,.6f),true),false);
        exitGate.SetActive(false);
        player=new GameObject("Игрок"); player.transform.position=lastCheckpoint;
        cc=player.AddComponent<CharacterController>(); cc.radius=.35f; cc.height=1.8f; cc.center=new Vector3(0,.9f,0); cc.stepOffset=.65f;
        var camGo=new GameObject("Камера от первого лица"); camGo.transform.SetParent(player.transform);
        camGo.transform.localPosition=new Vector3(0,1.58f,0); camGo.transform.localRotation=Quaternion.identity;
        view=camGo.AddComponent<Camera>(); view.fieldOfView=73; view.nearClipPlane=.05f; view.farClipPlane=180;
        camGo.tag="MainCamera";
        torch=camGo.AddComponent<Light>(); torch.type=LightType.Spot; torch.range=28; torch.spotAngle=62;
        torch.intensity=4.6f; torch.color=new Color(.9f,.94f,1f);
        sounds=player.AddComponent<AudioSource>(); sounds.playOnAwake=false; sounds.spatialBlend=0;
        chaseMusic=player.AddComponent<AudioSource>(); chaseMusic.playOnAwake=false; chaseMusic.loop=true; chaseMusic.spatialBlend=0; chaseMusic.volume=.31f; chaseMusic.clip=ChaseTheme();
        pickSound=Tone(660,.16f); hitSound=Tone(160,.12f); bossSound=Tone(110,.35f);
        Show("MEM ISLAND — ПРИКЛЮЧЕНИЕ НАЧИНАЕТСЯ",5);
    }
    void SetupShop()
    {
        var wood=Mat("shopwood",new Color(.48f,.27f,.19f));
        Cube("Лавка Котости",new Vector3(-4,.65f,2),new Vector3(7,1.3f,4),wood);
        Cube("Крыша лавки",new Vector3(-4,3.5f,2),new Vector3(8,.6f,5),Mat("roof",new Color(.43f,.19f,.3f)));
        for(int k=-1;k<=1;k+=2) Cube("Опора",new Vector3(-4+k*3.1f,2.2f,3.5f),new Vector3(.3f,3,.3f),wood);
        cat=new GameObject("КОТОСТЬ / торговец"); cat.transform.position=new Vector3(-4,1.2f,3.1f);
        var fur=Mat("catfur",new Color(.95f,.64f,.3f));
        ChildBall(cat,"Тело",new Vector3(0,0,0),new Vector3(1.35f,1.3f,1.1f),fur);
        ChildBall(cat,"Голова",new Vector3(0,.86f,.1f),Vector3.one*.9f,fur);
        ChildCube(cat,"Левое ухо",new Vector3(-.33f,1.37f,.1f),new Vector3(.32f,.48f,.29f),fur);
        ChildCube(cat,"Правое ухо",new Vector3(.33f,1.37f,.1f),new Vector3(.32f,.48f,.29f),fur);
        ChildBall(cat,"Глаза 1",new Vector3(-.24f,.94f,.52f),Vector3.one*.15f,Mat("eyes",new Color(.06f,.09f,.12f)));
        ChildBall(cat,"Глаза 2",new Vector3(.24f,.94f,.52f),Vector3.one*.15f,Mat("eyes",new Color(.06f,.09f,.12f)));
        Label(cat.transform,"КОТОСТЬ",new Vector3(0,2.3f,0),new Color(1,.91f,.56f));
    }
    void SetupCage()
    {
        cage=new GameObject("Клетка Мелстроя"); cage.transform.position=new Vector3(7,0,6);
        var iron=Mat("iron",new Color(.5f,.56f,.63f));
        ChildCube(cage,"Основание",new Vector3(0,.1f,0),new Vector3(4,.2f,4),iron);
        ChildCube(cage,"Крыша",new Vector3(0,3f,0),new Vector3(4,.2f,4),iron);
        for(int i=-2;i<=2;i++)
        {
            float q=i*.88f;
            ChildCube(cage,"Решётка перед",new Vector3(q,1.6f,-1.9f),new Vector3(.12f,3,.12f),iron);
            ChildCube(cage,"Решётка сзади",new Vector3(q,1.6f,1.9f),new Vector3(.12f,3,.12f),iron);
            ChildCube(cage,"Решётка слева",new Vector3(-1.9f,1.6f,q),new Vector3(.12f,3,.12f),iron);
            ChildCube(cage,"Решётка справа",new Vector3(1.9f,1.6f,q),new Vector3(.12f,3,.12f),iron);
        }
        var shirt=Mat("shirt",new Color(.86f,.17f,.15f));
        ChildBall(cage,"Человек в клетке",new Vector3(0,1.3f,0),new Vector3(.95f,1.5f,.8f),shirt);
        ChildBall(cage,"Голова",new Vector3(0,2.32f,0),Vector3.one*.75f,Mat("skin",new Color(.91f,.69f,.48f)));
        Label(cage.transform,"МЕЛСТРОЙ",new Vector3(0,4f,0),Color.white);
    }
    void SetupMemeEnemies()
    {
        AddEnemy("6-7",new Vector3(5,1.2f,26),new Color(.85f,.25f,.78f),2.7f);
        AddEnemy("ЧЕРЕМША",new Vector3(-5,1.2f,20),new Color(.67f,.82f,.25f),1.8f);
        AddEnemy("БУРМАЛДА",new Vector3(6,1.2f,70),new Color(.91f,.45f,.21f),2.1f);
        AddEnemy("ПЛАКСИК",new Vector3(-4,2.8f,124),new Color(.4f,.76f,.95f),1.5f);
        AddEnemy("МЫШЬ ПОЗОРА",new Vector3(5,3.2f,141),new Color(.6f,.58f,.67f),2.7f);
        AddEnemy("СЕМГА",new Vector3(-2,3.4f,150),new Color(.96f,.54f,.37f),2.0f);
        AddEnemy("ПУХОСОС",new Vector3(0,3.8f,184),new Color(.75f,.67f,.98f),1.7f);
        AddEnemy("БРДЫЩ",new Vector3(-6,1.1f,230),new Color(.7f,.3f,.35f),2.3f);
        // These are level-layout placeholders; exact placement will be directed by the player.
    }
    void AddEnemy(string name, Vector3 at, Color c, float speed)
    {
        GameObject obj=new GameObject(name); obj.transform.position=at;
        ChildBall(obj,"Мем-враг",Vector3.zero,new Vector3(1.35f,1.5f,1.15f),Mat("enemy"+name,c));
        ChildBall(obj,"Глаз L",new Vector3(-.3f,.21f,.56f),Vector3.one*.2f,Mat("white",Color.white));
        ChildBall(obj,"Глаз R",new Vector3(.3f,.21f,.56f),Vector3.one*.2f,Mat("white",Color.white));
        Label(obj.transform,name,new Vector3(0,1.45f,0),new Color(1,.9f,.68f),45);
        enemies.Add(new Enemy{root=obj,name=name,speed=speed,nextHit=0});
    }
    void SetupIceCube()
    {
        GameObject iceCube=new GameObject("ЛУК ЧЕСНОК — кубик льда с глазами");
        iceCube.transform.position=new Vector3(-6,4,132);
        ChildCube(iceCube,"Ледяное тело",Vector3.zero,new Vector3(2.5f,2.5f,2.5f),Mat("cubeIce",new Color(.52f,.86f,1f)));
        ChildBall(iceCube,"Левый глаз",new Vector3(-.55f,.35f,1.3f),new Vector3(.47f,.47f,.2f),Mat("white",Color.white));
        ChildBall(iceCube,"Правый глаз",new Vector3(.55f,.35f,1.3f),new Vector3(.47f,.47f,.2f),Mat("white",Color.white));
        ChildBall(iceCube,"Зрачок L",new Vector3(-.55f,.35f,1.43f),Vector3.one*.18f,Mat("eyes",Color.black));
        ChildBall(iceCube,"Зрачок R",new Vector3(.55f,.35f,1.43f),Vector3.one*.18f,Mat("eyes",Color.black));
        Label(iceCube.transform,"ЛУК ЧЕСНОК",new Vector3(0,2.2f,0),Color.cyan,43);
        enemies.Add(new Enemy{root=iceCube,name="ЛУК ЧЕСНОК",speed=0f});
    }
    void SetupCollectibles()
    {
        // Exactly 14 available before the boss, and exactly one appears after victory.
        Vector3[] positions = {
            new Vector3(8,1,4), new Vector3(12,1,14),new Vector3(-11,1,21),new Vector3(-8,1,29),
            new Vector3(6,1,44),new Vector3(-5,1,58),new Vector3(5,1,75),new Vector3(-6,1,89),
            new Vector3(5,1,103),new Vector3(-4,1.8f,114),new Vector3(5,2.6f,131),new Vector3(-5,3.3f,147),
            new Vector3(-2.4f,4,155),new Vector3(-2.4f,3.6f,182)
        };
        for(int i=0;i<positions.Length;i++) AddTelifon(positions[i],i+1);
    }
    GameObject AddTelifon(Vector3 at,int n)
    {
        var root=new GameObject("ТЕЛИФОН "+n); root.transform.position=at;
        var body=Mat("telifon",new Color(.25f,.83f,.84f),true);
        ChildBall(root,"Тело",Vector3.zero,new Vector3(.95f,.65f,1.1f),body);
        ChildBall(root,"Левый глаз",new Vector3(-.2f,.31f,.52f),Vector3.one*.21f,Mat("white",Color.white));
        ChildBall(root,"Правый глаз",new Vector3(.2f,.31f,.52f),Vector3.one*.21f,Mat("white",Color.white));
        for(int k=-1;k<=1;k+=2)
        {
            for(int j=0;j<4;j++)
            {
                var leg=ChildCube(root,"Лапа",new Vector3(k*(.57f+j*.08f),-.08f,(j-1.5f)*.28f),new Vector3(.55f,.12f,.12f),body);
                leg.transform.localRotation=Quaternion.Euler(0,k*(j-1.5f)*23f,k*20f);
            }
        }
        Label(root.transform,"ТЕЛИФОН",new Vector3(0,1.12f,0),Color.cyan,35);
        collectibles.Add(new Collectible {root=root,place=at,taken=false});
        return root;
    }
    void SetupBoss()
    {
        bossMat=Mat("boss",new Color(.28f,.69f,.95f),true);
        boss=new GameObject("ВОЗДУХАН — ФИНАЛЬНЫЙ БОСС"); boss.transform.position=new Vector3(0,6,236);
        ChildBall(boss,"Облако",Vector3.zero,new Vector3(3.1f,2.5f,2.4f),bossMat);
        ChildBall(boss,"Облако L",new Vector3(-1.1f,.3f,0),new Vector3(1.9f,2.1f,2),bossMat);
        ChildBall(boss,"Облако R",new Vector3(1.1f,.3f,0),new Vector3(1.9f,2.1f,2),bossMat);
        ChildBall(boss,"Глаза L",new Vector3(-.6f,.3f,1.26f),Vector3.one*.32f,Mat("eyes",Color.black));
        ChildBall(boss,"Глаза R",new Vector3(.6f,.3f,1.26f),Vector3.one*.32f,Mat("eyes",Color.black));
        Label(boss.transform,"ВОЗДУХАН",new Vector3(0,2.75f,0),Color.white,50);
    }
    GameObject ChildBall(GameObject parent,string name, Vector3 local,Vector3 scale,Material m)
    {
        var o=Ball(name,parent.transform.position+local,scale,m);
        o.transform.SetParent(parent.transform);o.transform.localPosition=local;return o;
    }
    GameObject ChildCube(GameObject parent,string name, Vector3 local,Vector3 scale,Material m)
    {
        var o=Cube(name,parent.transform.position+local,scale,m,false);
        o.transform.SetParent(parent.transform);o.transform.localPosition=local;return o;
    }
    AudioClip Tone(float pitchHz,float seconds)
    {
        const int sampleRate=22050; int n=(int)(sampleRate*seconds);
        var data=new float[n];
        for(int i=0;i<n;i++)
        {
            float t=(float)i/sampleRate;
            float env=Mathf.Pow(1f-(float)i/n,2);
            data[i]=Mathf.Sin(2*Mathf.PI*pitchHz*t)*env*.22f;
        }
        var c=AudioClip.Create("sfx"+pitchHz,n,1,sampleRate,false);c.SetData(data,0);return c;
    }
    AudioClip ChaseTheme()
    {
        // Original placeholder loop. The meme's copyrighted recording is NOT shipped.
        const int sr=22050; int length=sr*4;float[] d=new float[length];
        float[] notes={220,261.63f,293.66f,261.63f,220,196,174.61f,196};
        for(int i=0;i<length;i++)
        {
            float t=(float)i/sr;int step=((int)(t*4))%notes.Length;
            float nt=t%(.25f), env=Mathf.Exp(-nt*10);
            float phase=2*Mathf.PI*notes[step]*t;
            float beat=(t%(.5f)<.07f?Mathf.Sin(2*Mathf.PI*85*t)*.18f:0f);
            d[i]=(Mathf.Sin(phase)*.15f+Mathf.Sin(phase*2)*.08f)*env+beat;
        }
        var clip=AudioClip.Create("Original chase preview loop",length,1,sr,false);clip.SetData(d,0);return clip;
    }
    void Show(string s,float seconds=4) {banner=s;bannerUntil=Time.time+seconds;}
    void Speak(string s,float seconds=6) {subtitle=s;Show(s,seconds);}
    void Update()
    {
        if(ending) return;
        ReadControls(); MovePlayer(); TickInteractions(); TickTelifons(); TickEnemies(); TickChase(); TickBoss(); TickShots(); TickEnding();
    }
    void ReadControls()
    {
        stick=Vector2.zero;
        bool fingerLook=false;
        foreach(Touch t in Input.touches)
        {
            if(t.position.x<Screen.width*.42f)
            {
                Vector2 p=new Vector2(t.position.x,t.position.y);
                stick=Vector2.ClampMagnitude((p-stickCenter)/84f,1f);
            }
            else if(t.position.y>Screen.height*.34f)
            {
                yaw+=t.deltaPosition.x*.18f;
                pitch-=t.deltaPosition.y*.14f;
                fingerLook=true;
            }
        }
        if(!fingerLook && Input.GetMouseButton(1))
        {
            yaw+=Input.GetAxis("Mouse X")*2.6f;
            pitch-=Input.GetAxis("Mouse Y")*2.6f;
        }
        pitch=Mathf.Clamp(pitch,-65f,69f);
        player.transform.rotation=Quaternion.Euler(0,yaw,0);
        view.transform.localRotation=Quaternion.Euler(pitch,0,0);
    }
    void MovePlayer()
    {
        float x=Input.GetAxisRaw("Horizontal"), z=Input.GetAxisRaw("Vertical");
        if(stick.magnitude>.09f){x=stick.x;z=stick.y;}
        Vector3 move=player.transform.right*x+player.transform.forward*z;
        if(move.sqrMagnitude>1) move.Normalize();
        float speed=chaseStarted&&!chaseDone ? 11f : 6f;
        if(cc.isGrounded) { vertical.y=-1.4f; if(jumpPressed || Input.GetKeyDown(KeyCode.Space)) vertical.y=8.0f; }
        vertical.y += -20f*Time.deltaTime;
        cc.Move((move*speed+vertical)*Time.deltaTime);
        jumpPressed=false;
        if(player.transform.position.y<-5f) Respawn("Ты упал! Возвращаемся к контрольной точке.");
        float zpos=player.transform.position.z;
        if(zpos>96 && zpos<153) {stage="ЛЕДЯНАЯ ГОРА";lastCheckpoint=new Vector3(0,3,100);}
        else if(zpos>=153 && zpos<207) {stage="ПАРКУР";lastCheckpoint=new Vector3(0,4,155);}
        else if(zpos>=207&&zpos<260) {stage="АРЕНА";lastCheckpoint=new Vector3(0,3,211);}
        else if(zpos>=260) stage="ФИНАЛ";
        else if(zpos>34) stage="ДОРОГА";
        else if(zpos>13) stage="ЛЕС С ВРАГАМИ";
        else stage="СПАВН / МИРНЫЕ NPC";
    }
    void Respawn(string why)
    {
        hearts=3; cc.enabled=false;player.transform.position=lastCheckpoint;cc.enabled=true;
        vertical=Vector3.zero;Show(why,3);
        if(chaseStarted&&!chaseDone){chaseStarted=false;chaseMusic.Stop(); if(pursuer!=null)Destroy(pursuer);}
    }
    float DistanceXZ(Vector3 a,Vector3 b) {a.y=b.y;return Vector3.Distance(a,b);}
    void TickInteractions()
    {
        if(!talkPressed && !Input.GetKeyDown(KeyCode.E)) return;
        talkPressed=false;
        float catDist=Vector3.Distance(player.transform.position,cat.transform.position);
        if(catDist<6f)
        {
            if(!talkedCat)
            {
                talkedCat=true;
                Speak("КОТОСТЬ: Собери 15 Телифонов! На острове полно ненормальных мемов!",7);
            }
            else if(!gotSlipper)
            {
                gotSlipper=true;
                Speak("КОТОСТЬ: А, точно! Тебе надо оружие. Всё, что у меня есть — этот тапок. Ладно, иди!",8);
                sounds.PlayOneShot(pickSound);
            }
            else Speak("КОТОСТЬ: Ну чего стоишь? Иди за Телифонами!",4);
            return;
        }
        if(Vector3.Distance(player.transform.position,cage.transform.position)<6f)
        {
            talkedCage=true;
            Speak("МЕЛСТРОЙ: Здаров! Меня поймал Интерпол, но скоро меня заберут птенцы!",8);
            return;
        }
        if(bossStarted&&!bossDead) Speak("ВОЗДУХАН: Поймай меня, когда приземлюсь!",3);
        else Show("Здесь не с кем разговаривать",2);
    }
    void TickTelifons()
    {
        for(int i=0;i<collectibles.Count;i++)
        {
            var c=collectibles[i]; if(c.taken)continue;
            c.root.transform.Rotate(0,55*Time.deltaTime,0);
            c.root.transform.position=c.place+Vector3.up*Mathf.Sin(Time.time*2+i)*.16f;
            if(Vector3.Distance(player.transform.position,c.root.transform.position)<1.65f)
            {
                c.taken=true;c.root.SetActive(false);count++;sounds.PlayOneShot(pickSound);
                Show("ТЕЛИФОН НАЙДЕН!   "+count+" / 15",2.6f);
                if(count==14) Show("14/15! Последний Телифон у Воздухана. Иди на арену!",6);
            }
        }
    }
    void TickEnemies()
    {
        if(attackPressed || Input.GetKeyDown(KeyCode.F)) {Attack();attackPressed=false;}
        foreach(var en in enemies)
        {
            if(en.defeated||en.root==null)continue;
            Vector3 dist=player.transform.position-en.root.transform.position;
            float d=DistanceXZ(player.transform.position,en.root.transform.position);
            if(d<13 && d>1.3f && en.speed>0)
            {
                Vector3 direction=new Vector3(dist.x,0,dist.z).normalized;
                en.root.transform.position+=direction*en.speed*Time.deltaTime;
                en.root.transform.rotation=Quaternion.LookRotation(direction);
            }
            if(d<1.35f && Mathf.Abs(dist.y)<2.3f && Time.time>en.nextHit)
            {
                Damage(en.name+" ударил тебя!");en.nextHit=Time.time+2;
            }
        }
    }
    void Attack()
    {
        if(!gotSlipper) {Show("Сначала получи тапок у Котости!",2);return;}
        if(Time.time<nextAttack)return;
        nextAttack=Time.time+.48f; sounds.PlayOneShot(hitSound);Show("ШЛЁП!",.4f);
        if(bossStarted&&!bossDead)
        {
            float d=DistanceXZ(player.transform.position,boss.transform.position);
            bool grounded=bossClock%9f>5f;
            Vector3 to=(boss.transform.position-player.transform.position).normalized;
            if(d<5.2f && Vector3.Dot(player.transform.forward,to)>.15f)
            {
                if(!grounded) {Show("ВОЗДУХАН ЛЕТАЕТ! Жди приземления!",2);return;}
                bossHP--;sounds.PlayOneShot(bossSound);
                if(bossHP==5)
                {
                    bossLine="ТЫ МЕНЯ БЕСИШЬ!!!";bossLineUntil=Time.time+5f;
                    bossMat.color=new Color(.95f,.16f,.43f);Show("ФАЗА 2: СУПЕР ВОЗДУХАН!",5);
                }
                if(bossHP<=0) BossVictory();
                return;
            }
        }
        for(int i=0;i<enemies.Count;i++)
        {
            var e=enemies[i];if(e.defeated)continue;
            Vector3 delta=e.root.transform.position-player.transform.position;
            if(delta.magnitude<3.2f && Vector3.Dot(player.transform.forward,delta.normalized)>.2f)
            {
                e.defeated=true;e.root.SetActive(false);Show(e.name+" получил тапком!",2);break;
            }
        }
    }
    void Damage(string cause)
    {
        if(Time.time<nextDamage)return;
        nextDamage=Time.time+1.5f;hearts--;sounds.PlayOneShot(hitSound);
        if(hearts<=0) Respawn("СЕРДЕЧКИ ЗАКОНЧИЛИСЬ! Возрождение. Телифоны сохраняются.");
        else Show(cause+" Осталось сердечек: "+hearts,2);
    }
    void TickChase()
    {
        float z=player.transform.position.z;
        if(!chaseDone&&!chaseStarted&&z>36&&z<93)
        {
            chaseStarted=true;chaseStartTime=Time.time;
            pursuer=Ball("Неизвестный преследователь",player.transform.position-new Vector3(0,0,14),new Vector3(3,3.5f,3),Mat("chaser",new Color(.9f,.16f,.32f),true));
            Show("БЕГИ! RUN FROM YOUR DEMONS!",4);
            musicChase=true;chaseMusic.Play(); // Original placeholder tune, not the meme recording.
        }
        if(chaseStarted&&!chaseDone)
        {
            if(z>94)
            {
                chaseDone=true;chaseStarted=false;musicChase=false;chaseMusic.Stop();
                if(pursuer!=null)Destroy(pursuer);Show("Тишина... Как будто ничего не было.",5);
            }
            else if(pursuer!=null)
            {
                Vector3 dir=(player.transform.position-pursuer.transform.position);
                dir.y=0;
                if(dir.sqrMagnitude>.1f)pursuer.transform.position+=dir.normalized*8.8f*Time.deltaTime;
                if(dir.magnitude<1.7f && Time.time>nextDamage) Damage("Тебя догнал преследователь!");
            }
        }
    }
    void TickBoss()
    {
        float z=player.transform.position.z;
        if(!bossStarted&&!bossDead&&z>215f)
        {
            if(count<14)
            {
                if(z>217) {cc.enabled=false;player.transform.position=new Vector3(0,3,212);cc.enabled=true;Show("Сначала собери 14 Телифонов! Сейчас: "+count+"/14",4);}
                return;
            }
            bossStarted=true;bossClock=0;bossHP=10;nextShot=Time.time+3;
            bossLine="ВОТ МЫ И ВСТРЕТИЛИСЬ! НО ТЫ МЕНЯ НЕ ПОБЕДИШЬ!";
            bossLineUntil=Time.time+6;
            sounds.PlayOneShot(bossSound);
        }
        if(!bossStarted||bossDead)return;
        bossClock+=Time.deltaTime;
        float phase=bossClock%9f;
        bool landed=phase>5f;
        float y=landed?1.5f:6f+Mathf.Sin(Time.time*2f)*.7f;
        boss.transform.position=new Vector3(Mathf.Sin(Time.time*.36f)*4f,y,235+Mathf.Cos(Time.time*.3f)*2f);
        boss.transform.Rotate(0,30*Time.deltaTime,0);
        if(!landed&&Time.time>nextShot)
        {
            nextShot=Time.time+(bossHP<=5?1.15f:2.2f);
            FireAir();
            if(bossHP<=5) {FireAir(.26f); FireAir(-.26f);}
        }
    }
    void FireAir(float angle=0)
    {
        Vector3 source=boss.transform.position;source.y=2;
        Vector3 dir=player.transform.position+Vector3.up-source;dir.y=0;
        dir=Quaternion.Euler(0,angle*120f,0)*dir.normalized;
        GameObject b=Ball("Воздушный заряд",source,Vector3.one*.75f,Mat("air",new Color(.44f,.9f,1f),true));
        shots.Add(new AirShot{root=b,direction=dir,ttl=6});
    }
    void TickShots()
    {
        for(int i=shots.Count-1;i>=0;i--)
        {
            var s=shots[i];s.ttl-=Time.deltaTime;
            s.root.transform.position+=s.direction*(bossHP<=5?13:9)*Time.deltaTime;
            if(Vector3.Distance(player.transform.position+Vector3.up,s.root.transform.position)<1.2f)
            {Damage("Воздухан попал воздушным зарядом!");s.ttl=0;}
            if(s.ttl<=0){Destroy(s.root);shots.RemoveAt(i);}
        }
    }
    void BossVictory()
    {
        bossDead=true;bossStarted=false;
        bossLine="ЭТО НЕ КОНЕЦ! Я ВЕРНУСЬ!";bossLineUntil=Time.time+5;
        Show("ВОЗДУХАН ТЕЛЕПОРТИРОВАЛСЯ!",5);
        Vector3 where=boss.transform.position;boss.SetActive(false);
        AddTelifon(new Vector3(0,2,235),15);
        exitGate.SetActive(true);
        for(int i=shots.Count-1;i>=0;i--) Destroy(shots[i].root);
        shots.Clear();
        sounds.PlayOneShot(pickSound);
    }
    void TickEnding()
    {
        if(!bossDead || count<15)return;
        if(player.transform.position.z>252 && player.transform.position.z<265)
        {
            cc.enabled=false;player.transform.position=new Vector3(0,2,278);cc.enabled=true;
            Show("ФИНАЛЬНЫЙ ОСТРОВ — ПОДОЙДИ К КОТОСТИ",8);
            cat.transform.position=new Vector3(0,1.2f,289);
            exitGate.SetActive(false);
        }
        if(player.transform.position.z>284&& Vector3.Distance(player.transform.position,cat.transform.position)<8)
        {
            ending=true;Show("MEM ISLAND — THE END",30);
        }
    }
    void OnGUI()
    {
        var scl=new Vector2(Screen.width/1280f,Screen.height/720f);
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scl.x,scl.y,1));
        var label=GUI.skin.label;label.fontSize=24;label.normal.textColor=Color.white;
        GUI.skin.box.fontSize=22; GUI.skin.button.fontSize=21;
        GUI.Box(new Rect(12,12,430,110)," ");
        GUI.Label(new Rect(25,17,405,38),"MEM ISLAND     "+stage);
        GUI.Label(new Rect(25,55,420,34),"ТЕЛИФОНЫ: "+count+" / 15      "+new string('♥',hearts)+new string('♡',3-hearts));
        GUI.Label(new Rect(25,88,380,30),"🔦 Фонарик  •  "+(gotSlipper?"🩴 Тапок":"Нет оружия"));
        if(bossStarted&&!bossDead)
        {
            GUI.Box(new Rect(380,18,520,69),"ВОЗДУХАН     "+(bossHP<=5?"СУПЕРФОРМА":"ФАЗА 1"));
            var old=GUI.color;GUI.color=new Color(.24f,.1f,.17f);
            GUI.DrawTexture(new Rect(406,57,470,18),Texture2D.whiteTexture);
            GUI.color=bossHP<=5?new Color(1,.21f,.43f):new Color(.5f,.8f,1f);
            GUI.DrawTexture(new Rect(406,57,470*bossHP/10f,18),Texture2D.whiteTexture);GUI.color=old;
        }
        if(bossLineUntil>Time.time) GUI.Box(new Rect(185,122,910,58),"ВОЗДУХАН: "+bossLine);
        if(bannerUntil>Time.time) GUI.Box(new Rect(265,575,750,46),banner);
        else if(!gotSlipper) GUI.Box(new Rect(210,575,860,46),"Сначала поговори с Котостью ДВА раза, чтобы получить тапок!");
        if(!ending)
        {
            GUI.Box(new Rect(20,566,240,140),"ДВИЖЕНИЕ");
            GUI.Label(new Rect(53,623,190,44),"◉   ◀   ▲   ▶");
            if(GUI.Button(new Rect(1120,560,140,138),"🩴 УДАР")) attackPressed=true;
            if(GUI.Button(new Rect(968,590,136,108),"ПРЫЖОК")) jumpPressed=true;
            if(GUI.Button(new Rect(1094,430,160,105),"ГОВОРИТЬ")) talkPressed=true;
            GUI.Label(new Rect(450,665,500,30),"ПК: WASD, мышь (ПКМ), E, F, пробел");
        }
        else
        {
            GUI.Box(new Rect(210,143,860,442),"MEM ISLAND  —  THE END\n\nКОТОСТЬ: Ты смог это сделать! Ты победил Воздухана!\n\nМелстроя забрали птенцы. Теперь он в безопасности.\n\nНо это не конец... У Воздухана есть СЕКРЕТНАЯ БАЗА!\n\nНо пока это конец.\n\nСпасибо за игру!\nСоздатель: Negelinho\n\nПродолжение следует?.. ");
            if(GUI.Button(new Rect(484,600,315,65),"ИГРАТЬ СНАЧАЛА")) UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}

public class FaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if(Camera.main!=null)
        {
            transform.rotation=Quaternion.LookRotation(transform.position-Camera.main.transform.position);
        }
    }
}
