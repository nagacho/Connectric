using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class BoardManager : MonoBehaviour {

    // 定数
    public const int BOARD_ALL_NUM = 25;
    public const int BOARD_WIDTH_NUM = 5;
    public const int BOARD_HEIGHT_NUM = 5;
    public const float between = 1.85f;
    public Vector3 vStartPos = new Vector3(-3.7f,1.2f,0.0f);
    public const float DEBUG_COLOR = 0.0f;
    public const int RAINBOM_HIT = 5;

    // ピースタイプ
    public enum INSTRUMENT_TYPE {
        GUITAR = 0,
        DRUM,
        VOCAL,
        DJ,
        RAINBOW,
        RAINBOM,
        TIME,
        MAX
    };

    // リンクターゲットタイプ
    public enum TARGET_FORM {
        TATE = 0,
        YOKO,
        MAX
    }

    // 構造体
    public struct PANEL_DATA {
        public GameObject obj;       // 
        public int arrayWidthNum;    // 配列番号
        public int arrayHeightNum;   // 配列番号
        public int typeNum;          // 属性
        public bool mouseFlag;       // キャプチャーされているか
        public bool moveFlag;        // 動かせるか
        public bool linkflag;        // リンクしているかの確認
        public bool deletePrepareFrag;
    };

    // 変数
    public static GameObject[,] Boards = new GameObject[BOARD_WIDTH_NUM,BOARD_HEIGHT_NUM];
    public static PANEL_DATA[,] Boardpieces = new PANEL_DATA[BOARD_WIDTH_NUM,BOARD_HEIGHT_NUM];

    private List<GameObject> linkFlames = new List<GameObject>();
    private List<GameObject> onpuList = new List<GameObject>();

    // 変数宣言
    [SerializeField] private GameObject onpuPerfomance;
    [SerializeField] private GameObject board;
    [SerializeField] private GameObject linkFlame;
    [SerializeField] private GameObject[] piece = new GameObject[(int)INSTRUMENT_TYPE.MAX];
    [SerializeField] private bool[] flag = new bool[BOARD_ALL_NUM];
    [SerializeField] private int[,] Target = new int[2,2];        // リンクテスト
    [SerializeField] private int combo = 0;                       // コンボ数
    private GameObject game_manager;
    private GameObject mouse;
    private GameObject sound;

    // 確率 調整用
    private int guitarNum = 0;
    private int drumNum = 0;
    private int vocalNum = 0;
    private int djNum = 0;
    private int rainbowNum = 0;
    private int rainbomNum = 0;
    private int rainbowCounter = 0;
    private bool rainbomFlag = false;

    // スキル
    private bool hibikaSkilFrag = false;


    //---------------------------------------------------
    // コンボ数の取得
    //---------------------------------------------------
    public int Combo {
        get { return combo; }
        set { combo = value; }
    }

    //===================================================
    // Use this for initialization
    //===================================================
    void Start () {
        
        InitializeRate();   // ピース出現確率の設定

        CreateBoard();
        game_manager = GameObject.Find("GameManager");
        mouse = GameObject.Find("Mouse");
        sound = GameObject.Find("SoundManager");
    }

    //===================================================
    // Update is called once per frame
    //===================================================
    void Update () {

        // ポーズ、クリア画面、ゲームオーバーいずれなら更新しない
        if(game_manager.GetComponent<GameManager>().IsGameClear ||
            game_manager.GetComponent<GameManager>().IsGameOver ||
               game_manager.GetComponent<GameManager>().IsPause) { return; }


        MoveMausePiece();

        // 削除準備
        if(game_manager.GetComponent<GameManager>().IsBeatChange) {
            PieceDeletePrepare();

            // レインボムの出現確率発生調整
            if (combo >= RAINBOM_HIT)
            {
                rainbomFlag = true;
                Debug.Log("koreja!!!!11");
            }
        }

        // 小さくする＆ピースを消す(演出)
        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                if(Boardpieces[width,height].deletePrepareFrag) {
                    Boardpieces[width,height].obj.GetComponent<Piece>().Small();
                    if(Boardpieces[width,height].obj.GetComponent<Piece>().DeleteFrag) {
                        PieceDelete(width,height);
                    }
                }
            }
        }

        // ピースを消した後の音符演出の削除タイミング
        for(int i = 0;i < onpuList.Count;i++) {
            if(onpuList[i].GetComponent<Onpu_perfo>().GetTime() >= 1.0f) {
                Debug.Log("音符デリート");
                onpuList[i].GetComponent<Onpu_perfo>().DeleteOnpu();
                onpuList.RemoveAt(i);
                i--;
            }
        }

       // 各キャラクターのスキル発動
        if(game_manager.GetComponent<GameManager>().IsSkillMode) {
            // ＊＊＊＊カナデのスキル発動条件＊＊＊＊
            if (game_manager.GetComponent<GameManager>().FocusCharacter == 0) {
                SkillActiveTime();
            }

            // ＊＊＊＊セイラのスキル発動条件＊＊＊＊
            if (game_manager.GetComponent<GameManager>().FocusCharacter == 1)
            {

            }

            // ＊＊＊＊ヒビカのスキル発動条件＊＊＊＊
            if (!hibikaSkilFrag && game_manager.GetComponent<GameManager>().FocusCharacter == 2)
            {
                rainbomFlag = true;
                hibikaSkilFrag = true;
                Debug.Log("ヒビカ");
            }
            else if(hibikaSkilFrag && game_manager.GetComponent<GameManager>().FocusCharacter != 2)
            {
                hibikaSkilFrag = false;
            }
        }



        Replenishment();        // 補充
        LinkDo();

        // それぞれのピース数
        //Debug.Log("ギターのピース数　　　" + guitarNum);
        //Debug.Log("ドラムのピース数　　　" + drumNum);
        //Debug.Log("ボーカルのピース数　　" + vocalNum);
        //Debug.Log("キーボードのピース数　" + djNum);
        //Debug.Log("レインボーのピース数　　" + rainbowNum);
        //Debug.Log("レインボー出現タイミング　" + rainbowCounter);

    }

    //-------------------------------------------------------
    // 配置ボードの生成
    //-------------------------------------------------------
    private void CreateBoard () {

        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {

                // 確率調整
                int obj_num = RandomPieceInitialize2();

                // ボード（あたり判定）
                Boards[width,height] = Instantiate(board,new Vector3(vStartPos.x + between * width,vStartPos.y - between * height,0.0f),Quaternion.identity);

                // ピースの生成
                CreatePiece(width,height,obj_num);

                // デバッグ用
                flag[width + height * 5] = Boardpieces[width,height].moveFlag;
            }
        }
    }

    //-------------------------------------------------------
    // 入れ替え可能ピースの設定
    //-------------------------------------------------------
    private void SetMovepiece () {

        Color cyan = new Color(0.0f,1.0f,1.0f,DEBUG_COLOR);

        //念のため初期化
        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                Boardpieces[width,height].moveFlag = false;
            }
        }

        // 入れ替え
        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                if(Boardpieces[width,height].mouseFlag) {
                    // 左上角
                    if(width == 0 && height == 0) {
                        Boardpieces[1,0].moveFlag = true;
                        Boardpieces[0,1].moveFlag = true;

                        // デバッグ用
                        Boards[1,0].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[0,1].GetComponent<SpriteRenderer>().color = cyan;

                        // 斜めも対応
                        Boardpieces[1,1].moveFlag = true;
                        Boards[1,1].GetComponent<SpriteRenderer>().color = cyan;
                    }

                    // 右上角
                    else if(width == (BOARD_WIDTH_NUM - 1) && height == 0) {
                        Boardpieces[BOARD_WIDTH_NUM - 2,0].moveFlag = true;
                        Boardpieces[BOARD_WIDTH_NUM - 1,1].moveFlag = true;

                        // デバッグ用
                        Boards[BOARD_WIDTH_NUM - 2,0].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[BOARD_WIDTH_NUM - 1,1].GetComponent<SpriteRenderer>().color = cyan;

                        // 斜めも対応
                        Boardpieces[BOARD_WIDTH_NUM - 2,1].moveFlag = true;
                        Boards[BOARD_WIDTH_NUM - 2,1].GetComponent<SpriteRenderer>().color = cyan;

                    }

                    // 左下角
                    else if(width == 0 && height == (BOARD_HEIGHT_NUM - 1)) {
                        Boardpieces[0,BOARD_HEIGHT_NUM - 2].moveFlag = true;
                        Boardpieces[1,BOARD_HEIGHT_NUM - 1].moveFlag = true;

                        // デバッグ用
                        Boards[0,BOARD_HEIGHT_NUM - 2].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[1,BOARD_HEIGHT_NUM - 1].GetComponent<SpriteRenderer>().color = cyan;

                        // 斜めも対応
                        Boardpieces[1,BOARD_HEIGHT_NUM - 2].moveFlag = true;
                        Boards[1,BOARD_HEIGHT_NUM - 2].GetComponent<SpriteRenderer>().color = cyan;

                    }

                    // 右下角
                    else if(width == (BOARD_WIDTH_NUM - 1) && height == (BOARD_HEIGHT_NUM - 1)) {
                        Boardpieces[BOARD_WIDTH_NUM - 2,BOARD_HEIGHT_NUM - 1].moveFlag = true;
                        Boardpieces[BOARD_WIDTH_NUM - 1,BOARD_HEIGHT_NUM - 2].moveFlag = true;

                        // デバッグ用
                        Boards[BOARD_WIDTH_NUM - 2,BOARD_HEIGHT_NUM - 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[BOARD_WIDTH_NUM - 1,BOARD_HEIGHT_NUM - 2].GetComponent<SpriteRenderer>().color = cyan;

                        // 斜めも対応
                        Boardpieces[BOARD_WIDTH_NUM - 2,BOARD_HEIGHT_NUM - 2].moveFlag = true;
                        Boards[BOARD_WIDTH_NUM - 2,BOARD_HEIGHT_NUM - 2].GetComponent<SpriteRenderer>().color = cyan;
                    }

                    // 上列
                    else if((width != 0 && height == 0) || (width != (BOARD_WIDTH_NUM - 1) && height == 0)) {
                        Boardpieces[width - 1,height].moveFlag = true;
                        Boardpieces[width + 1,height].moveFlag = true;
                        Boardpieces[width,height + 1].moveFlag = true;

                        // デバッグ用
                        Boards[width - 1,height].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width + 1,height].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width,height + 1].GetComponent<SpriteRenderer>().color = cyan;

                        // 斜めも対応
                        Boardpieces[width - 1,height + 1].moveFlag = true;
                        Boardpieces[width + 1,height + 1].moveFlag = true;
                        Boards[width - 1,height + 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width + 1,height + 1].GetComponent<SpriteRenderer>().color = cyan;
                    }

                    // 下列
                    else if((width != 0 && height == (BOARD_HEIGHT_NUM - 1)) || (width != (BOARD_WIDTH_NUM - 1) && height == (BOARD_HEIGHT_NUM - 1))) {
                        Boardpieces[width - 1,height].moveFlag = true;
                        Boardpieces[width + 1,height].moveFlag = true;
                        Boardpieces[width,height - 1].moveFlag = true;

                        // デバッグ用
                        Boards[width - 1,height].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width + 1,height].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width,height - 1].GetComponent<SpriteRenderer>().color = cyan;

                        // 斜めも対応
                        Boardpieces[width - 1,height - 1].moveFlag = true;
                        Boardpieces[width + 1,height - 1].moveFlag = true;
                        Boards[width - 1,height - 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width + 1,height - 1].GetComponent<SpriteRenderer>().color = cyan;
                    }

                    // 左列
                    else if((width == 0 && height != 0) || (width == 0 && height == (BOARD_HEIGHT_NUM - 1))) {
                        Boardpieces[width,height - 1].moveFlag = true;
                        Boardpieces[width,height + 1].moveFlag = true;
                        Boardpieces[width + 1,height].moveFlag = true;

                        // デバッグ用
                        Boards[width,height - 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width,height + 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width + 1,height].GetComponent<SpriteRenderer>().color = cyan;

                        // 斜めも対応
                        Boardpieces[width + 1,height - 1].moveFlag = true;
                        Boardpieces[width + 1,height + 1].moveFlag = true;
                        Boards[width + 1,height - 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width + 1,height + 1].GetComponent<SpriteRenderer>().color = cyan;
                    }

                    // 右列
                    else if((width == (BOARD_WIDTH_NUM - 1) && height != 0) || (width == (BOARD_WIDTH_NUM - 1) && height == (BOARD_HEIGHT_NUM - 1))) {
                        Boardpieces[width,height - 1].moveFlag = true;
                        Boardpieces[width,height + 1].moveFlag = true;
                        Boardpieces[width - 1,height].moveFlag = true;

                        // デバッグ用
                        Boards[width,height - 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width,height + 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width - 1,height].GetComponent<SpriteRenderer>().color = cyan;

                        // 斜めも対応
                        Boardpieces[width - 1,height - 1].moveFlag = true;
                        Boardpieces[width - 1,height + 1].moveFlag = true;
                        Boards[width - 1,height - 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width - 1,height + 1].GetComponent<SpriteRenderer>().color = cyan;
                    }

                    // 四か所できる
                    else {
                        Boardpieces[width - 1,height].moveFlag = true;
                        Boardpieces[width + 1,height].moveFlag = true;
                        Boardpieces[width,height - 1].moveFlag = true;
                        Boardpieces[width,height + 1].moveFlag = true;

                        // デバッグ用
                        Boards[width - 1,height].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width + 1,height].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width,height - 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width,height + 1].GetComponent<SpriteRenderer>().color = cyan;

                        // 斜めの対応
                        Boardpieces[width - 1,height - 1].moveFlag = true;
                        Boardpieces[width + 1,height - 1].moveFlag = true;
                        Boardpieces[width - 1,height + 1].moveFlag = true;
                        Boardpieces[width + 1,height + 1].moveFlag = true;
                        Boards[width - 1,height - 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width + 1,height - 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width - 1,height + 1].GetComponent<SpriteRenderer>().color = cyan;
                        Boards[width + 1,height + 1].GetComponent<SpriteRenderer>().color = cyan;
                    }

                }
            }
        }
    }

    //-------------------------------------------------------
    // マウスが持っているオブジェクトを動かす
    //-------------------------------------------------------
    private void MoveMausePiece () {

        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                if(Boardpieces[width,height].mouseFlag) {
                    Boardpieces[width,height].obj.GetComponent<Transform>().position = mouse.GetComponent<Mouse>().CursolWorldPos;
                    //Debug.Log("objectのセット");
                    break;
                }
            }
        }

    }

    //-------------------------------------------------------
    //  ピースの入れ替え
    //-------------------------------------------------------
    public void Change () {

        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                //マウスと当たったやつの検索
                if(Boards[width,height].GetComponent<SpriteRenderer>().color == new Color(0.0f,0.0f,0.0f,DEBUG_COLOR)) {
                    if(Boardpieces[width,height].moveFlag) {
                        for(int height2 = 0;height2 < BOARD_HEIGHT_NUM;height2++) {
                            for(int width2 = 0;width2 < BOARD_WIDTH_NUM;width2++) {
                                if(Boardpieces[width2,height2].mouseFlag) {

                                    PANEL_DATA save = Boardpieces[width2,height2];
                                    Boardpieces[width2,height2] = Boardpieces[width,height];
                                    Boardpieces[width2,height2].obj.GetComponent<Transform>().position = Boards[width2,height2].GetComponent<Transform>().position;
                                    Boardpieces[width,height] = save;


                                    // パネル入れかえサウンド
                                    sound.GetComponent<SoundManager>().TriggerSE("puzzlemove");


                                    for(int height3 = 0;height3 < BOARD_HEIGHT_NUM;height3++) {
                                        for(int width3 = 0;width3 < BOARD_WIDTH_NUM;width3++) {
                                            Boards[width3,height3].GetComponent<SpriteRenderer>().color = new Color(1.0f,0.0f,1.0f,DEBUG_COLOR);
                                        }
                                    }
                                    SetMovepiece();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    //-------------------------------------------------------
    // マウスが持っているオブジェクトのセット
    //-------------------------------------------------------
    public void SetMouseObj () {
        Color Gray = new Color(0.5f,0.5f,0.5f,DEBUG_COLOR);

        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                if(Boards[width,height].GetComponent<SpriteRenderer>().color == Gray) {
                    if(Boardpieces[width,height].typeNum == (int)INSTRUMENT_TYPE.TIME) {
                        Boardpieces[width,height].obj.GetComponent<PieceTime>().Big();
                    } else {
                        Boardpieces[width,height].obj.GetComponent<Piece>().Big();
                    }


                    Boardpieces[width,height].obj.GetComponent<SpriteRenderer>().sortingOrder = 101;
                    Boardpieces[width,height].mouseFlag = true;
                    Boards[width,height].GetComponent<SpriteRenderer>().color = new Color(1.0f,0.0f,1.0f,DEBUG_COLOR);
                    SetMovepiece();
                    break;
                }
            }
        }
    }

    //-------------------------------------------------------
    // マウスが持っていたオブジェクトの解放
    //-------------------------------------------------------
    public void ReleaseMouseObj () {
        // ボードカラー初期化
        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                Boards[width,height].GetComponent<SpriteRenderer>().color = new Color(1.0f,0.0f,1.0f,DEBUG_COLOR);
            }
        }

        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                if(Boardpieces[width,height].mouseFlag) {
                    Boardpieces[width,height].obj.GetComponent<Transform>().position = Boards[width,height].GetComponent<Transform>().position;
                    Boardpieces[width,height].obj.GetComponent<SpriteRenderer>().sortingOrder = 100;
                    Boardpieces[width,height].mouseFlag = false;
                    break;
                }

            }
        }
    }

    //-------------------------------------------------------
    // リンクチェックの実行
    //-------------------------------------------------------
    private void LinkDo () {

        // ノーツリンクの取得
        Target = game_manager.GetComponent<GameManager>().GetLatestPieceLink();

        //Debug.Log(Target[0,0]);
        //Debug.Log(Target[0,1]);
        //Debug.Log(Target[1,0]);
        //Debug.Log(Target[1,1]);


        // 念のため初期化
        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                Boardpieces[width,height].linkflag = false;
            }
        }

        LinkFlameDelete();

        combo = Link();
    }

    //-------------------------------------------------------
    // リンクチェック
    //-------------------------------------------------------
    private int Link () {
        int linknum = 0;

        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                // 左上の一致
                if(Boardpieces[width,height].typeNum == Target[0,0] ||
                    Boardpieces[width,height].typeNum == (int)INSTRUMENT_TYPE.RAINBOW ||
                    Boardpieces[width,height].typeNum == (int)INSTRUMENT_TYPE.RAINBOM) {
                    //if(!Boardpieces[width, height].mouseFlag)
                    {
                        // 縦と判断
                        if(Target[1,0] == -1 && Target[1,1] == -1) {
                            if(height == (BOARD_HEIGHT_NUM - 1)) { continue; }
                            if(Boardpieces[width,height + 1].typeNum == Target[0,1] ||
                                Boardpieces[width,height + 1].typeNum == (int)INSTRUMENT_TYPE.RAINBOW ||
                                Boardpieces[width,height + 1].typeNum == (int)INSTRUMENT_TYPE.RAINBOM) {
                                //if(!Boardpieces[width, height + 1].mouseFlag) { continue; }
                                linknum++;
                                Boardpieces[width,height].linkflag = true;
                                Boardpieces[width,height + 1].linkflag = true;

                                // レインボム用リンク
                                if(Boardpieces[width,height].typeNum == (int)INSTRUMENT_TYPE.RAINBOM) {
                                    RainbomLink(width,height);
                                } else if(Boardpieces[width,height + 1].typeNum == (int)INSTRUMENT_TYPE.RAINBOM) {
                                    RainbomLink(width,height + 1);
                                }

                                linkFlames.Add(CreateLinkFlame_t(width,height));
                                continue;
                            }

                        }
                        // 横と判断
                        else if(Target[0,1] == -1 && Target[1,1] == -1) {

                            if(width == (BOARD_WIDTH_NUM - 1)) { continue; }
                            if(Boardpieces[width + 1,height].typeNum == Target[1,0] ||
                                Boardpieces[width + 1,height].typeNum == (int)INSTRUMENT_TYPE.RAINBOW ||
                                Boardpieces[width + 1,height].typeNum == (int)INSTRUMENT_TYPE.RAINBOM) {
                                //if (!Boardpieces[width + 1, height].mouseFlag) { continue; }
                                linknum++;
                                Boardpieces[width,height].linkflag = true;
                                Boardpieces[width + 1,height].linkflag = true;

                                // レインボム用リンク
                                if(Boardpieces[width,height].typeNum == (int)INSTRUMENT_TYPE.RAINBOM) {
                                    RainbomLink(width,height);
                                } else if(Boardpieces[width + 1,height].typeNum == (int)INSTRUMENT_TYPE.RAINBOM) {
                                    RainbomLink(width + 1,height);
                                }

                                linkFlames.Add(CreateLinkFlame_y(width,height));
                                continue;
                            }
                        }
                    }
                }
            }
        }

        return linknum;
    }

    //-------------------------------------------------------
    // ピース削除準備
    //-------------------------------------------------------
    private void PieceDeletePrepare () {
        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {

                // リンクフラグ参照押して削除準備に入る
                if(Boardpieces[width,height].linkflag) {
                    Boardpieces[width,height].deletePrepareFrag = true;
                    Boardpieces[width,height].obj.GetComponent<Piece>().SmallFrag = true;
                    //Debug.Log("削除準備");
                }
            }
        }
        LinkFlameDelete();      // リンク背景削除
        game_manager.GetComponent<GameManager>().IsBeatChange = false;
    }

    //-------------------------------------------------------
    // ピース削除
    //-------------------------------------------------------
    private void PieceDelete (int width,int height) {
        // ピースを消す前に演出用の音符を作成
        onpuList.Add(Instantiate(onpuPerfomance,Boardpieces[width,height].obj.GetComponent<Transform>().position,Quaternion.identity));
        onpuList[(onpuList.Count - 1)].GetComponent<Onpu_perfo>().SetColor(Boardpieces[width,height].typeNum);
        onpuList[(onpuList.Count - 1)].GetComponent<Onpu_perfo>().SetPos(Boardpieces[width,height].obj.GetComponent<Transform>().position);

        SubRate(Boardpieces[width,height].typeNum);
        Destroy(Boardpieces[width,height].obj);

        Boardpieces[width,height].obj = Instantiate(piece[(int)INSTRUMENT_TYPE.TIME],new Vector3(vStartPos.x + between * width,vStartPos.y - between * height,0.0f),Quaternion.identity);
        Boardpieces[width,height].arrayWidthNum = width;
        Boardpieces[width,height].arrayHeightNum = height;
        Boardpieces[width,height].typeNum = (int)INSTRUMENT_TYPE.TIME;
        Boardpieces[width,height].mouseFlag = false;
        Boardpieces[width,height].moveFlag = false;
        Boardpieces[width,height].linkflag = false;
        Boardpieces[width,height].deletePrepareFrag = false;

    }

    //-------------------------------------------------------
    // 補充
    //-------------------------------------------------------
    private void Replenishment () {

        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {

                int obj_num = 0;

                // タイムピースなら
                if (Boardpieces[width, height].typeNum == (int)INSTRUMENT_TYPE.TIME)
                {

                    // ０になったら
                    if (Boardpieces[width, height].obj.GetComponent<PieceTime>().FinAnim)
                    {

                        // 削除
                        Destroy(Boardpieces[width, height].obj);

                        //セイラがスキルを発動しているかどうかで補充を変更
                        if (game_manager.GetComponent<GameManager>().IsUsingSkill(1))
                        {
                            if (Random.Range(0, 2) == 0)
                            {
                                obj_num = (int)INSTRUMENT_TYPE.RAINBOW;
                            }
                            else
                            {
                                // 乱数調整
                                obj_num = RandomPieceUpdate();
                            }

                        }
                        else
                        {
                            // 乱数調整
                            obj_num = RandomPieceUpdate();
                        }

                        // ピースの生成
                        CreatePiece(width, height, obj_num);
                        rainbowCounter++;

                    }
                    else if (rainbomFlag)
                    {
                        // 削除
                        Destroy(Boardpieces[width, height].obj);
                        obj_num = (int)INSTRUMENT_TYPE.RAINBOM;
                        rainbomFlag = false;
                        Debug.Log("レインボム");

                        // ピースの生成
                        CreatePiece(width, height, obj_num);
                        rainbowCounter++;

                    }
                }
            }
        }
    }

    //-------------------------------------------------------
    // ピースデータの初期化
    //-------------------------------------------------------
    private void CreatePiece (int width,int height,int obj_num) {
        // ピース
        Boardpieces[width,height].obj = Instantiate(piece[obj_num],new Vector3(vStartPos.x + between * width,vStartPos.y - between * height,0.0f),Quaternion.identity);
        Boardpieces[width,height].arrayWidthNum = width;
        Boardpieces[width,height].arrayHeightNum = height;
        Boardpieces[width,height].typeNum = obj_num;
        Boardpieces[width,height].mouseFlag = false;
        Boardpieces[width,height].moveFlag = false;
        Boardpieces[width,height].linkflag = false;
        Boardpieces[width,height].deletePrepareFrag = false;

        AddRate(Boardpieces[width,height].typeNum);
    }

    //-------------------------------------------------------
    // リンクフレーム（縦）
    //-------------------------------------------------------
    private GameObject CreateLinkFlame_t (int width,int height) {
        GameObject link_flame;
        Vector3 linkFlamePos = Boards[width,height].GetComponent<Transform>().position;
        linkFlamePos.y -= 1.0f;
        link_flame = Instantiate(linkFlame,linkFlamePos,Quaternion.identity);
        link_flame.GetComponent<SpriteRenderer>().sortingOrder = 1 + linkFlames.Count;
        return link_flame;
    }

    //-------------------------------------------------------
    // リンクフレーム（横）
    //-------------------------------------------------------
    private GameObject CreateLinkFlame_y (int width,int height) {
        GameObject link_flame;
        Vector3 linkFlamePos = Boards[width,height].GetComponent<Transform>().position;
        linkFlamePos.x += 1.0f;
        Quaternion linkFlameRot = Quaternion.Euler(0.0f,0.0f,90.0f);
        link_flame = Instantiate(linkFlame,linkFlamePos,linkFlameRot);
        link_flame.GetComponent<SpriteRenderer>().sortingOrder = 1 + linkFlames.Count;
        return link_flame;
    }

    //-------------------------------------------------------
    // リンクフレーム削除
    //-------------------------------------------------------
    private void LinkFlameDelete () {
        for(int i = 0;i < linkFlames.Count;i++) {
            Destroy(linkFlames[i]);
        }
        linkFlames.Clear();
    }

    //-------------------------------------------------------
    // スキル発動（タイム）
    //-------------------------------------------------------
    private void SkillActiveTime () {
        //Debug.Log("スキル発動");
        for(int height = 0;height < BOARD_HEIGHT_NUM;height++) {
            for(int width = 0;width < BOARD_WIDTH_NUM;width++) {
                if(Boardpieces[width,height].typeNum == (int)INSTRUMENT_TYPE.TIME) {
                    Boardpieces[width,height].obj.GetComponent<PieceTime>().FinAnim = true;
                    Replenishment();
                }
            }
        }
    }

    //-------------------------------------------------------
    // パズル確率初期化
    //-------------------------------------------------------
    private void InitializeRate () {
        guitarNum = 0;
        drumNum = 0;
        vocalNum = 0;
        djNum = 0;
        rainbowNum = 0;
        rainbomNum = 0;
        rainbowCounter = 0;
        rainbomFlag = false;
    }

    //-------------------------------------------------------
    // パズル確率 加算
    //-------------------------------------------------------
    private void AddRate (int num) {
        switch(num) {
            case (int)INSTRUMENT_TYPE.GUITAR:
            guitarNum++;
            break;

            case (int)INSTRUMENT_TYPE.DRUM:
            drumNum++;
            break;

            case (int)INSTRUMENT_TYPE.VOCAL:
            vocalNum++;
            break;

            case (int)INSTRUMENT_TYPE.DJ:
            djNum++;
            break;

            case (int)INSTRUMENT_TYPE.RAINBOW:
            rainbowNum++;
            break;

            case (int)INSTRUMENT_TYPE.RAINBOM:
            rainbomNum++;
            break;

            default:
            break;
        }
    }

    //-------------------------------------------------------
    // パズル確率 減算
    //-------------------------------------------------------
    private void SubRate (int num) {
        switch(num) {
            case (int)INSTRUMENT_TYPE.GUITAR:
            guitarNum--;
            break;

            case (int)INSTRUMENT_TYPE.DRUM:
            drumNum--;
            break;

            case (int)INSTRUMENT_TYPE.VOCAL:
            vocalNum--;
            break;

            case (int)INSTRUMENT_TYPE.DJ:
            djNum--;
            break;

            case (int)INSTRUMENT_TYPE.RAINBOW:
            rainbowNum--;
            break;

            case (int)INSTRUMENT_TYPE.RAINBOM:
            rainbomNum--;
            break;

            default:
            break;
        }
    }

    //-------------------------------------------------------
    // パズル乱数調整(初期化)
    //-------------------------------------------------------
    private int RandomPieceInitialize2 () {
        int randomNum = 0;
        bool[] checkNum = { (guitarNum >= 4),(drumNum >= 4),(vocalNum >= 4),(djNum >= 4) };
        List<int> candidate = new List<int>();

        for(int i = 0;i < checkNum.Length;i++) {
            if(!checkNum[i]) {
                candidate.Add(i);
            }
        }

        if(candidate.Count > 0) {
            int rand = Random.Range(0,candidate.Count);
            randomNum = candidate[rand];
        } else {
            randomNum = Random.Range(0,(int)INSTRUMENT_TYPE.MAX - 3);
        }

        return randomNum;
    }

    //-------------------------------------------------------
    // パズル乱数調整(更新時)
    //-------------------------------------------------------
    private int RandomPieceUpdate () {

        int randomNum = 0;
        bool[] checkNum = { (guitarNum > 0),(drumNum > 0),(vocalNum > 0),(djNum > 0),(rainbowCounter < 20),(!rainbomFlag) };
        List<int> candidate = new List<int>();

        for(int i = 0;i < checkNum.Length;i++) {
            if(!checkNum[i]) {
                candidate.Add(i);
            }
        }

        if(candidate.Count > 0) {
            int rand = Random.Range(0,candidate.Count);
            randomNum = candidate[rand];
        } else {
            randomNum = Random.Range(0,(int)INSTRUMENT_TYPE.MAX - 3);
        }

        // レインボー生成タイミングの初期化
        if(randomNum == (int)INSTRUMENT_TYPE.RAINBOW) {
            rainbowCounter = 0;
        }

        // レインボム生成を一つに制限
        if(randomNum == (int)INSTRUMENT_TYPE.RAINBOM) {
            rainbomFlag = false;
        }

        return randomNum;
    }

    //-------------------------------------------------------
    // レインボムリンク用
    //-------------------------------------------------------
    private void RainbomLink (int width,int height) {
        // 四つの方向チェック
        //                         左サイド　　　        右サイド　　　               上サイド　　           下サイド
        bool[] checkSlide = { (width - 1 >= 0),(width + 1 < BOARD_WIDTH_NUM),(height - 1 >= 0),(height + 1 < BOARD_HEIGHT_NUM) };

        // 左上
        if(checkSlide[0] && checkSlide[2]) {
            if(Boardpieces[width - 1,height - 1].typeNum != (int)INSTRUMENT_TYPE.TIME) {
                Boardpieces[width - 1,height - 1].linkflag = true;
            }

        }
        // 左
        if(checkSlide[0]) {
            if(Boardpieces[width - 1,height].typeNum != (int)INSTRUMENT_TYPE.TIME) {
                Boardpieces[width - 1,height].linkflag = true;
            }
        }
        // 左下
        if(checkSlide[0] && checkSlide[3]) {
            if(Boardpieces[width - 1,height + 1].typeNum != (int)INSTRUMENT_TYPE.TIME) {
                Boardpieces[width - 1,height + 1].linkflag = true;
            }
        }

        // 右上
        if(checkSlide[1] && checkSlide[2]) {
            if(Boardpieces[width + 1,height - 1].typeNum != (int)INSTRUMENT_TYPE.TIME) {
                Boardpieces[width + 1,height - 1].linkflag = true;
            }
        }
        // 右
        if(checkSlide[1]) {
            if(Boardpieces[width + 1,height].typeNum != (int)INSTRUMENT_TYPE.TIME) {
                Boardpieces[width + 1,height].linkflag = true;
            }
        }
        // 右下
        if(checkSlide[1] && checkSlide[3]) {
            if(Boardpieces[width + 1,height + 1].typeNum != (int)INSTRUMENT_TYPE.TIME) {
                Boardpieces[width + 1,height + 1].linkflag = true;
            }
        }

        // 上
        if(checkSlide[2]) {
            if(Boardpieces[width,height - 1].typeNum != (int)INSTRUMENT_TYPE.TIME) {
                Boardpieces[width,height - 1].linkflag = true;
            }
        }

        // 下
        if(checkSlide[3]) {
            if(Boardpieces[width,height + 1].typeNum != (int)INSTRUMENT_TYPE.TIME) {
                Boardpieces[width,height + 1].linkflag = true;
            }
        }
    }
}
