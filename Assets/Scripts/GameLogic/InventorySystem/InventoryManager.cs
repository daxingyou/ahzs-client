/*----------------------------------------------------------------
// Copyright (C) 2013 广州，爱游
//
// 模块名：InventoryManager
// 创建者：Steven Yang
// 修改者列表：Joe Mo
// 创建日期：2013-3-5
// 模块描述：玩家背包管理器
//----------------------------------------------------------------*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mogo.GameData;
using Mogo.Util;
using Mogo.Game;

using System.Text;

public class InventoryManager
{
    #region 根据00道具策划编号规则.docx
    public enum ItemType
    {
        ITEM = 1,
        ARMOR = 2,
        WEAPON = 3,
        JEWEL = 4,
        RUNE = 5,
        SPIRIT = 6
    };

    private enum ItemSubType
    {
        DRUG = 1,
        STUFF = 2
    };

    private enum JewelSubType
    {
        COREJEWEL = 1,
        HITJEWEL = 2,
        MOREHURTJEWEL = 3,
        MOREHITJEWEL = 4,
        WRECKHITJEWEL = 5,
        LIFEJEWEL = 6
    };

    private enum RuneSubType
    {
        CORERUNE = 1,
        HITRUNE = 2,
        MOREHURTRUNE = 3,
        MOREHITRUNE = 4,
        WRECKHITRUNE = 5,
        LIFERUNE = 6
    };

    private enum ItemQuality
    {
        WHITE = 1,
        GREEN = 2,
        BLUE = 3,
        PURPLE = 4,
        ORANGE = 5
    };

    #endregion
    private EntityMyself myself;
    #region 事件
    public const string ON_INVENTORY_SORT = "InventoryManager.ON_INVENTORY_SORT";
    public const string ON_ITEMS_SELECTED = "InventoryManager.ON_ITEMS_SELECTED";
    public const string ON_EQUIP = "InventoryManager.ON_EQUIP";
    public const string ON_REMOVE_EQUIP = "InventoryManager.ON_REMOVE_EQUIP";
    public const string ON_EQUIP_GRID_UP = "InventoryManager.OnEquipmentGridUp";
    public const string ON_INVENTORY_SHOW = "InventoryManager.ON_INVENTORY_SHOW";
    public const string ON_EQUIP_COMSUME_SHOW = "InventoryManager.ON_EQUIP_COMSUME_SHOW";
    public const string ON_STRENTHEN_EQUIP = "InventoryManager.ON_STRENTHEN_EQUIP";
    public const string ON_EQUIP_INSET = "InventoryManager.ON_EQUIP_INSET";
    public const string ON_PACKAGE_SWITCH = "InventoryManager.ON_PACKAGE_SWITCH";
    public const string ON_EQUIP_SHOW = "InventoryManager.ON_EQUIP_SHOW";
    public const string ON_INSET_JEWEL = "InventoryManager.ON_INSET_JEWEL";
    public const string ON_COMPOSE = "InventoryManager.ON_COMPOSE";
    public const string ON_DECOMPOSE = "InventoryManager.ON_DECOMPOSE";
    #endregion

    //策划说每种分类都等于一个独立的背包,要有整理功能，要记录空的格子，所以会有idx
    public const int ITEM_TYPE_EQUIPMENT = 1; //装备分类
    public const int ITEM_TYPE_JEWEL = 2; //宝石分类
    public const int ITEM_TYPE_MATERIAL = 3; //元素分类
    public const int ITEM_TYPE_RUNE = 4;
    public const int ITEM_TYPE_ONEQUIP = 5;

    const int GRID_COUNT_PER_PAGE = 10;
    public const int SLOT_NUM = 11;
    public const int JEWEL_SORTED_GRID_NUM = 42;
    public const int BAG_CAPACITY = 40;

    private Dictionary<int, float> m_propCdDic = new Dictionary<int, float>();
    private Dictionary<int, Dictionary<int, ItemParent>> m_itemsInBag = new Dictionary<int, Dictionary<int, ItemParent>>();//包中道具
    private Dictionary<int, ItemEquipment> m_itemsOnEquip = new Dictionary<int, ItemEquipment>();//装备中的物件(key为部位id)
    public Dictionary<int, ItemEquipment> EquipOnDic { get { return m_itemsOnEquip; } }

    public Dictionary<int, ItemParent> JewelInBag { get { return m_itemsInBag[ITEM_TYPE_JEWEL]; } }
    public EntityMyself Myself { get { return myself; } }

    public List<ItemEquipment> EquipmentInBagList
    {
        get
        {
            List<ItemEquipment> list = new List<ItemEquipment>();
            foreach (ItemParent item in m_itemsInBag[ITEM_TYPE_EQUIPMENT].Values)
            {
                if (item.itemType != 1) continue;
                list.Add((ItemEquipment)item);
            }
            return list;
        }
    }
    int m_iCurrentTagIndex = 1;
    public int CurrentTagIndex
    {
        get
        {
            return m_iCurrentTagIndex;
        }
        set
        {
            MenuUIViewManager.Instance.HandlePackageTabChange(m_iCurrentTagIndex, value);
            m_iCurrentTagIndex = value;
        }
    }

    private ItemParent m_selectedItem;
    public int m_versionId;

    //private int m_money = 10000;
    //private int m_diamond = 10000;

    //public int Diamond { get { return m_diamond; } }
    //public int Gold { get { return m_money; } }
    ComposeManager composeManager;
    InsetManager insetManager;
    DecomposeManager decomposeManager;

    private View m_currentView = View.PlayerEquipment;
    public View CurrentView
    {
        get { return m_currentView; }
        set
        {
            m_currentView = value;

            if (EquipmentUIViewManager.Instance != null)
            {
                switch (m_currentView)
                {
                    case View.BodyEnhanceView:
                        EquipmentUIViewManager.Instance.CurrentDownTab = (int)EquipmentUITab.StrenthTab;
                        break;
                    case View.InsetView:
                        EquipmentUIViewManager.Instance.CurrentDownTab = (int)EquipmentUITab.InsetTab;
                        break;
                    case View.ComposeView:
                        EquipmentUIViewManager.Instance.CurrentDownTab = (int)EquipmentUITab.ComposeTab;
                        break;
                    case View.DecomposeView:
                        EquipmentUIViewManager.Instance.CurrentDownTab = (int)EquipmentUITab.DecomposeTab;
                        break;
                }
            }
        }
    }

    public View m_currentPackageView = View.PlayerEquipment;//
    public View m_currentEquipmentView = View.BodyEnhanceView;//;

    public bool m_isNeedAutoSortBag = false;

    int m_pageIndexEquipment = 0;
    int m_pageIndexJewel = 0;
    int m_pageIndexMaterial = 0;

    static public InventoryManager Instance;
    private EquipUpgradeManager equipUpgradeManager;
    private bool m_isCaching = false;
    private Dictionary<string, int> m_getSthDic = new Dictionary<string, int>();

    public enum View
    {
        None = -1,
        InsetView = 0,
        ComposeView = 1,
        DecomposeView = 2,
        BodyEnhanceView = 3,
        PackageView = 4,
        PlayerEquipment = 5
    }

    public InventoryManager(EntityMyself _myself)
    {
        Instance = this;
        myself = _myself;
        m_itemsInBag[ITEM_TYPE_EQUIPMENT] = new Dictionary<int, ItemParent>();
        m_itemsInBag[ITEM_TYPE_JEWEL] = new Dictionary<int, ItemParent>();
        m_itemsInBag[ITEM_TYPE_MATERIAL] = new Dictionary<int, ItemParent>();
        InitData();
        m_versionId = 0;
        composeManager = new ComposeManager(this);
        insetManager = new InsetManager(this);
        decomposeManager = new DecomposeManager(m_itemsInBag[ITEM_TYPE_EQUIPMENT], this);
        equipUpgradeManager = new EquipUpgradeManager();
        m_versionId++;
        AddListeners();


    }

    private void InitData()
    {
        //通知服务器，客户端需要初始化数据
        List<ItemParentInstance> list = new List<ItemParentInstance>();


        if (ServerProxy.Instance.GetType() == typeof(LocalProxy))
        {
            //4个宝石，2个已插，2个放背包
            ItemParentInstance item7 = new ItemJewelInstance() { templeId = 1411031, gridIndex = 0, stack = 2, bagType = ITEM_TYPE_JEWEL };
            ItemParentInstance item8 = new ItemJewelInstance() { templeId = 1411041, gridIndex = 3, stack = 3, bagType = ITEM_TYPE_JEWEL };

            List<int> jewelSlots = new List<int>();
            jewelSlots.Add(-1);
            ItemParentInstance item1;
            ItemParentInstance item2;
            // 为去除警告暂时屏蔽以下代码
            //ItemParentInstance item3;
            //ItemParentInstance item4;
            ItemParentInstance item5;

            //4个装备
            switch (myself.vocation)
            {
                case Vocation.Warrior:
                    item1 = new ItemEquipmentInstance() { templeId = 1311015, gridIndex = 0, jewelSlots = jewelSlots, bagType = ITEM_TYPE_EQUIPMENT };
                    //item2 = new ItemEquipmentInstance() { templeId = 12101102, gridIndex = 1, bagType = ITEM_TYPE_EQUIPMENT };
                    //item3 = new ItemEquipmentInstance() { templeId = 12101103, gridIndex = 2, bagType = ITEM_TYPE_EQUIPMENT };
                    //item4 = new ItemEquipmentInstance() { templeId = 12101104, gridIndex = 3, bagType = ITEM_TYPE_EQUIPMENT };
                    item5 = new ItemEquipmentInstance() { templeId = 1321015, gridIndex = 4, bagType = ITEM_TYPE_EQUIPMENT };
                    list.Add(item1);
                    //list.Add(item2);
                    //list.Add(item3);
                    //list.Add(item4);
                    list.Add(item5);
                    break;
                case Vocation.Assassin:
                    item1 = new ItemEquipmentInstance() { templeId = 1331015, gridIndex = 0, jewelSlots = jewelSlots, bagType = ITEM_TYPE_EQUIPMENT };
                    item2 = new ItemEquipmentInstance() { templeId = 1341015, gridIndex = 1, bagType = ITEM_TYPE_EQUIPMENT };

                    list.Add(item1);
                    list.Add(item2);
                    break;
                case Vocation.Archer:
                    item1 = new ItemEquipmentInstance() { templeId = 1371015, gridIndex = 0, jewelSlots = jewelSlots, bagType = ITEM_TYPE_EQUIPMENT };
                    item2 = new ItemEquipmentInstance() { templeId = 1381015, gridIndex = 1, jewelSlots = jewelSlots, bagType = ITEM_TYPE_EQUIPMENT };
                    list.Add(item1);
                    list.Add(item2);
                    break;

            }


            list.Add(item7);
            list.Add(item8);
        }
        _InitData(list);

        LoggerHelper.Debug("InventoryReq");
        myself.RpcCall("InventoryReq");
    }

    private void _InitData(List<ItemParentInstance> listFromServer)
    {
        //根据从服务器得到的所有item实例初始化
        Dictionary<int, ItemParent> normalItems = new Dictionary<int, ItemParent>();
        Dictionary<int, ItemParent> jewelItems = new Dictionary<int, ItemParent>();
        Dictionary<int, ItemParent> spiritItems = new Dictionary<int, ItemParent>();

        foreach (ItemParentInstance itemInstance in listFromServer)
        {
            ItemParent item;
            switch (itemInstance.bagType)
            {

                case ITEM_TYPE_EQUIPMENT:
                    item = new ItemEquipment((ItemEquipmentInstance)itemInstance);
                    normalItems[item.gridIndex] = item;
                    break;
                case ITEM_TYPE_JEWEL:
                    item = new ItemJewel((ItemJewelInstance)itemInstance);

                    jewelItems[item.gridIndex] = item;
                    break;
                case ITEM_TYPE_MATERIAL:
                    //itemParent = new ItemJewel(itemInstance);
                    //spiritItems[itemParent.GridIndex] = itemParent;
                    break;
            }
        }
        m_itemsInBag[ITEM_TYPE_EQUIPMENT] = normalItems;
        m_itemsInBag[ITEM_TYPE_JEWEL] = jewelItems;
        m_versionId++;
    }

    void RefreshUI()
    {
        Mogo.Util.LoggerHelper.Debug("inventory RefreshUI()");
        if (BodyEnhanceManager.Instance == null) LoggerHelper.Debug("BodyEnhanceManager.Instance == null!");
        switch (CurrentView)
        {
            case View.BodyEnhanceView:
                //BodyEnhanceManager.Instance.RefreshUI();
                //Mogo.Util.LoggerHelper.Debug("BodyEnhanceManager.Instance.RefreshUI()");
                break;
            case View.ComposeView:
                //composeManager.RefreshUI();
                //Mogo.Util.LoggerHelper.Debug("composeManager.RefreshUI()");
                break;
            case View.DecomposeView:
                //decomposeManager.RefreshUI();
                //Mogo.Util.LoggerHelper.Debug("decomposeManager.RefreshUI()");
                break;
            case View.InsetView:
                //Mogo.Util.LoggerHelper.Debug("insetManager.RefreshUI()");
                //insetManager.RefreshUI();
                break;
            case View.PackageView:
                RefreshPackageUI();
                Mogo.Util.LoggerHelper.Debug("RefreshPackageUI");
                break;
            case View.PlayerEquipment:
                RefreshPlayerEquipmentInfoUI();
                //Mogo.Util.LoggerHelper.Debug("RefreshPlayerEquipmentInfoUI");
                break;
            default:
                //Mogo.Util.LoggerHelper.Debug("NOthing!!!");
                break;
        }
        //Mogo.Util.LoggerHelper.Debug("RefreshUI() done");
    }

    public void RefreshPackageUI()
    {
        if (MenuUIViewManager.Instance != null)
        {
            switch (CurrentTagIndex)
            {
                case ITEM_TYPE_EQUIPMENT: RefreshEquipPackage(); break;
                case ITEM_TYPE_JEWEL: RefreshJewelPackage(); break;
                case ITEM_TYPE_MATERIAL: RefreshMaterialPackage(); break;
            }

            MenuUILogicManager.Instance.SetInventoryGold((int)myself.gold);
            MenuUILogicManager.Instance.SetInventoryDiamond((int)myself.diamond);
        }
    }

    private void RefreshMaterialPackage()
    {
        List<ItemParent> materialList = new List<ItemParent>();

        foreach (ItemParent item in m_itemsInBag[ITEM_TYPE_MATERIAL].Values)
        {
            materialList.Add(item);
        }

        MenuUILogicManager.Instance.ResetInventoryItems(materialList);
    }

    private void RefreshJewelPackage()
    {
        LoggerHelper.Debug("RefreshJewelPackage");
        List<ItemParent> jewelList = new List<ItemParent>();
        foreach (ItemParent item in m_itemsInBag[ITEM_TYPE_JEWEL].Values)
        {
            jewelList.Add(item);
        }
        MenuUILogicManager.Instance.ResetInventoryItems(jewelList);
        LoggerHelper.Debug("RefreshJewelPackage done");
    }

    private void RefreshEquipPackage()
    {
        //LoggerHelper.Debug(m_itemsInBag[ITEM_TYPE_EQUIPMENT].Count);
        List<ItemParent> normalList = new List<ItemParent>();

        foreach (ItemParent item in m_itemsInBag[ITEM_TYPE_EQUIPMENT].Values)
        {
            normalList.Add(item);
        }
        LoggerHelper.Debug(normalList.Count);
        MenuUILogicManager.Instance.ResetInventoryItems(normalList);


        //显示"UP"标记
        foreach (ItemParent item in normalList)
        {
            if (IsEquipmentBetter(item.templateId))
            {
                MenuUILogicManager.Instance.ShowPackageGridUpSign(item.gridIndex, true);
            }
            else if (IsEquipmentBetter(item.templateId, false))
            {
                MenuUILogicManager.Instance.ShowPackageGridUpSignBL(item.gridIndex, true);
            }

        }
    }

    public void RefreshPlayerEquipmentInfoUI()
    {
        for (int i = 1; i <= SLOT_NUM; i++)
        {
            if (m_itemsOnEquip.ContainsKey(i))
            {
                SetPlayerEquipmentInfo(i, m_itemsOnEquip[i].icon, 0, m_itemsOnEquip[i].quality);
            }
            else
            {
                //因为没有卸装功能，为方便这里不刷新了，ui预先写死
                //SetPlayerEquipmentInfo(i, EquipSlotIcon.icons[i], 10);
            }
        }
    }

    /// <summary>
    /// 计算玩家身上装备总分
    /// </summary>
    public int CalPlayerEquipmentScore()
    {
        int totalScore = 0;
        for (int i = 1; i <= SLOT_NUM; i++)
        {
            if (m_itemsOnEquip.ContainsKey(i))
            {
                // 需要刷新Level值
                EquipTipViewData data = GetEquipInfoByItem(m_itemsOnEquip[i], MogoWorld.thePlayer.level);

                totalScore += m_itemsOnEquip[i].score;
                LoggerHelper.Debug("slot = " + i + "   score = " + m_itemsOnEquip[i].score + "   totalScore = " + totalScore);
            }
        }

        return totalScore;
    }

    /// <summary>
    /// 计算玩家身上装备镶嵌的宝石总分
    /// </summary>
    /// <returns></returns>
    public int CalPlayerJewelScore()
    {
        // 遍历玩家身上的宝石,通过宝石等级获得每个宝石的得分PowerScoreJewelData.GetJewelScoreByLevel()
        int scoreSum = 0;
        foreach (ItemEquipment equip in m_itemsOnEquip.Values)
        {
            if (equip.jewelSlots == null) continue;
            foreach (int jewelId in equip.jewelSlots)
            {
                if (jewelId <= 0) continue;
                if (!ItemJewelData.dataMap.ContainsKey(jewelId))
                {
                    LoggerHelper.Error("jewelId:" + jewelId + " does not exist!");
                    continue;
                }
                ItemJewelData jewel = ItemJewelData.dataMap.Get(jewelId);
                scoreSum += PowerScoreJewelData.GetJewelScoreByLevel(jewel.level);
            }

        }
        return scoreSum;
    }

    /// <summary>
    /// 所有部位的强化等级之和
    /// </summary>
    /// <returns></returns>
    public int CalBodyEnhanceScore()
    {
        if (BodyEnhanceManager.Instance == null || BodyEnhanceManager.Instance.myEnhance == null || BodyEnhanceManager.Instance.myEnhance.Count == 0)
            return 0;
        int sum = 0;
        foreach (int level in BodyEnhanceManager.Instance.myEnhance.Values)
        {
            sum += level;
        }
        return sum;
    }

    private void OnRemoveEquip()
    {
        if (m_itemsInBag[ITEM_TYPE_EQUIPMENT].Count == BAG_CAPACITY)
        {
            ShowEquipError(2);
            return;
        }
        if (ServerProxy.Instance.GetType() == typeof(LocalProxy))
        {
            //todo根据服务器传过来的参数修改好的已卸装备刷新客户端数据，这里暂在客户端处理
            ItemEquipment selectedItem = (ItemEquipment)this.m_selectedItem;
            //身下的卸下来
            m_itemsOnEquip.Remove(selectedItem.gridIndex);

            //放在背包中
            for (int i = 0; i < 10; i++)
            {
                LoggerHelper.Debug(i);
                if (m_itemsInBag[ITEM_TYPE_EQUIPMENT].ContainsKey(i)) continue;
                m_itemsInBag[ITEM_TYPE_EQUIPMENT][i] = selectedItem;
                selectedItem.gridIndex = i;
                selectedItem.bagType = ITEM_TYPE_EQUIPMENT;
                break;
            }

            //刷新ui
            EquipTipManager.Instance.CloseEquipTip();
            //MenuUIViewManager.Instance.ShowPackageDetailInfo(false);
            myself.RemoveEquip(selectedItem.templateId);

            m_versionId++;
            RefreshUI();
        }
        else
        {
            myself.RpcCall("RemoveEquipment", m_selectedItem.id, (byte)(m_selectedItem.gridIndex));
        }

    }

    private void RemoveEquip()
    {
    }

    public void OnEquipUpgradeIconUp(int _typeId)
    {
        Mogo.Util.LoggerHelper.Debug("typeId:" + _typeId);
        Mogo.Util.LoggerHelper.Debug("m_currentTagIndex:" + CurrentTagIndex);
        if (!m_itemsInBag[CurrentTagIndex].ContainsKey(_typeId)) return;
        ItemParent item = m_itemsInBag[CurrentTagIndex][_typeId];
        m_selectedItem = item;

        OnEquip();
    }

    //private void ShowEquipInfo(ItemParent item)
    //{
    //    ItemEquipment ie = (ItemEquipment)item;

    //    //view.SetEquipDetailInfoNeedLevel(data.levelNeed);
    //    //view.SetEquipDetailInfoGrowLevel(data.growLevel);
    //    //for (int i = 0; i < 9; i++)
    //    //{
    //    //    view.SetDiamondHoleInfo(data.holeInfos[i], i);
    //    //}
    //    //for (int i = 0; i < 4; i++)
    //    //{
    //    //    view.SetDiamondHoleInfo(data.jewelHoles[i], i + 9);
    //    //    view.ShowNewDiamondHoleIcon(i + 9, (data.jewelHoles[i] != EquipTipViewData.NONE_JEWEL));
    //    //}
    //    //view.SetEquipDetailInfoNeedJob(data.job);
    //    ie.level = ie.levelNeed;
    //    EquipTipViewData data = GetEquipInfoByItem(ie);
    //    MenuUIViewManager view = MenuUIViewManager.Instance;

    //    view.SetEquipDetailInfoImage(data.iconName);
    //    view.ShowEquipDetailInfoImageUsed(data.isEquipOn);
    //    view.SetEquipDetailInfoName(data.name);
    //    view.SetEquipDetailInfoImageBg(IconData.GetIconByQuality(ie.quality));


    //    List<string> attrList = ie.GetAttrDescriptionList(MogoWorld.thePlayer);
    //    List<string> jewelList = new List<string>();
    //    foreach (string s in data.jewelHoles)
    //    {
    //        if (s == "") continue;
    //        jewelList.Add(s);
    //    }
    //    MenuUIViewManager.Instance.ShowEquipInfoDetail(attrList, jewelList, ie.levelNeed + "", LanguageData.dataMap[ie.vocation].content);

    //}



    //public EquipTipViewData GetEquipInfoByItem(int _templateId, List<int> _jewelSlots)
    //{
    //    ItemEquipmentInstance instance = new ItemEquipmentInstance() { templeId = _templateId, jewelSlots = _jewelSlots };
    //    return GetEquipInfoByItem(new ItemEquipment(instance),);
    //}

    static public EquipTipViewData GetEquipInfoByItem(ItemEquipment ie, int level)
    {
        //LoggerHelper.Debug("GetEquipInfoByItem:" + ie.templateId+"!!!!!!!!!!!!!!");
        EquipTipViewData data = new EquipTipViewData();
        data.levelNeed = ie.levelNeed;
        //ie.level跟玩家level相关，不实时更新，需要使用时设置，其具体属性根据此值变化
        ie.level = MogoWorld.thePlayer.level;
        if (ie.level <= 0) ie.level = 99;
        data.growLevel = ie.level + "/" + ie.levelLimit;
        ie.level = ie.levelNeed;
        List<string> attrList = ie.GetAttrDescriptionList(level);
        int m = 0;
        for (m = 0; m < attrList.Count && m < 9; m++)
        {
            data.holeInfos[m] = attrList[m];
        }
        for (; m < 9; m++)
        {
            data.holeInfos[m] = "";
        }

        data.job = LanguageData.dataMap[ie.vocation].content;

        data.iconName = ie.icon;
        data.isEquipOn = (ie.bagType == ITEM_TYPE_ONEQUIP);
        for (int i = 0; i < 4; i++)
        {
            data.jewelHoles[i] = "";
        }
        if (ie.jewelSlotsType != null)
        {
            data.jewelSlotIcons = new List<string>();
            for (int i = 0; i < ie.jewelSlotsType.Count; i++)
            {
                data.jewelHoles[i] = LanguageData.GetContent(910);//+ ie.jewelSlotsType[i];
                data.jewelSlotIcons.Add(IconData.dataMap.Get(30018 + ie.jewelSlotsType[i]).path);
            }

            for (int i = 0; i < ie.jewelSlots.Count; i++)
            {
                int id = ie.jewelSlots[i];
                if (id == -1) continue;
                data.jewelSlotIcons[i] = IconData.dataMap.Get(30027 + ie.jewelSlotsType[i]).path;
                ItemJewelData jewel = ItemJewelData.dataMap[id];
                string jewelInfo = jewel.effectDescriptionStr + "";
                data.jewelHoles[i] = jewelInfo;
            }
        }

        data.name = ie.name;
        return data;
    }

    public class EquipTipViewData
    {
        public string name = "none";
        public const string NONE_JEWEL = "none";
        public int levelNeed;
        public string growLevel;
        public string[] holeInfos = new string[9];
        public string iconName;
        public string job;
        public bool isEquipOn;
        public List<string> jewelHoles;
        public List<string> jewelSlotIcons;
        public EquipTipViewData()
        {
            jewelHoles = new List<string>();
            jewelHoles.Add(NONE_JEWEL);
            jewelHoles.Add(NONE_JEWEL);
            jewelHoles.Add(NONE_JEWEL);
            jewelHoles.Add(NONE_JEWEL);
        }
    }

    private void OnEquip()
    {
        //LoggerHelper.Error("(byte)m_selectedItem.gridIndex:" + (byte)m_selectedItem.gridIndex);

        //byte errorId = 0;
        //if ((errorId = (byte)CanEquip(m_selectedItem.templateId)) != 0)
        //{
        //    ShowEquipError(errorId);
        //    return;
        //}
        if (ServerProxy.Instance.GetType() == typeof(LocalProxy))
        {
            Equip();
        }
        else
        {
            //LoggerHelper.Error("equip!!!!");
            Mogo.Util.LoggerHelper.Debug("RpcCall ExchangeEquipment:" + "m_selectedItem:" + m_selectedItem.id + ", (byte)m_selectedItem:" + m_selectedItem.gridIndex);
            myself.RpcCall("ExchangeEquipment", m_selectedItem.id, (byte)m_selectedItem.gridIndex);
        }



    }

    /// <summary>
    /// </summary>
    /// <param name="id"></param>
    /// <returns>
    ///SUCCESS = 0, 
    ///EQUIPMENT_NOT_EXISTED = 1, //装备不存在
    ///EQUIPMENT_INVENTORY_FULL = 2, //背包满了
    ///EQUIPMENT_NOT_EXISTED_IN_CFG_TBL = 3,//系统不存在这个装备
    ///EQUIPMENT_NOT_EXISTED_IN_ATTRI_TBL = 4,//系统属性表检索不存在
    ///LEVEL_NOT_ENOUGH = 5,
    ///DATA_UNMATCH = 6,
    ///VOCATION_UNMATCH = 7,
    ///NOT_KNOWN = 8,
    ///</returns>
    private int CanEquip(int id)
    {
        if (myself.level < ItemEquipmentData.dataMap[id].levelNeed) return 5;
        if ((sbyte)myself.vocation != ItemEquipmentData.dataMap[id].vocation) return 7;
        return 0;
    }

    private void Equip()
    {

        //LoggerHelper.Debug("Equip:" + this.m_selectedItem.templateId);

        ItemEquipment selectedItem = (ItemEquipment)this.m_selectedItem;
        Mogo.Util.LoggerHelper.Debug("selectedItem.templateId:" + selectedItem.templateId);
        m_itemsInBag[ITEM_TYPE_EQUIPMENT].Remove(selectedItem.gridIndex);
        MenuUILogicManager.Instance.RemoveInventoryGridItem(selectedItem.gridIndex);

        if (selectedItem.type == (sbyte)EquipType.Weapon)
        {
            selectedItem.gridIndex = 11;
        }
        else
        {
            selectedItem.gridIndex = selectedItem.type;
        }

        if (m_itemsOnEquip.ContainsKey(selectedItem.gridIndex))
        {
            ItemEquipment old = m_itemsOnEquip[selectedItem.gridIndex];
            m_selectedItem = old;
            OnRemoveEquip();
        }
        //else
        //{

        //}
        //这里temp++暂时处理
        m_itemsOnEquip[selectedItem.gridIndex] = (ItemEquipment)selectedItem;
        selectedItem.bagType = ITEM_TYPE_ONEQUIP;
        MenuUIViewManager.Instance.ShowPackageDetailInfo(false);
        MenuUIViewManager.Instance.ShowPacakgeCurrentDetailInfo(false);
        myself.Equip(selectedItem.templateId);

        m_versionId++;
        RefreshUI();



        //myself.RpcCall("JewelAddEquiReq", selectedItem.templateId);
    }

    public void SetPlayerEquipmentInfo(int slot, string iconName, int color = 0, int quality = 1)
    {
        EquipSlot type = (EquipSlot)slot;
        MenuUIViewManager instance = MenuUIViewManager.Instance;
        if (instance == null) return;
        if (slot > 9)
            slot -= 1;
        string name = EquipSlotName.names[slot - 1];
        if (iconName != IconData.none)
            name = "";

        string qulityIcon = IconData.GetIconByQuality(quality);
        switch (type)
        {
            case EquipSlot.Belt:
                instance.SetPlayerBeltFG(iconName, color);
                instance.SetPlayerBeltBG(qulityIcon);
                instance.SetPlayerBeltName(name);
                break;
            case EquipSlot.Cuirass:
                instance.SetPlayerBreastPlateFG(iconName, color);
                instance.SetPlayerBreastPlateBG(qulityIcon);
                instance.SetPlayerBreastPlateName(name);
                break;
            case EquipSlot.Glove:
                instance.SetPlayerHandGuardFG(iconName, color);
                instance.SetPlayerHandGuardBG(qulityIcon);
                instance.SetPlayerHandGuardName(name);
                break;
            case EquipSlot.Head:
                instance.SetPlayerHeadEquipFG(iconName, color);
                instance.SetPlayerHeadEquipBG(qulityIcon);
                instance.SetPlayerHeadEquipName(name);
                break;
            case EquipSlot.Cuish:
                instance.SetPlayerCuishFG(iconName, color);
                instance.SetPlayerCuishBG(qulityIcon);
                instance.SetPlayerCuishName(name);
                break;
            case EquipSlot.Neck:
                instance.SetPlayerNecklaceFG(iconName, color);
                instance.SetPlayerNecklaceBG(qulityIcon);
                instance.SetPlayerNecklaceName(name);
                break;
            case EquipSlot.LeftRing:
                instance.SetPlayerRingLeftFG(iconName, color);
                instance.SetPlayerRingLeftBG(qulityIcon);
                instance.SetPlayerRingLeftName(name);
                break;
            case EquipSlot.RightRing:
                instance.SetPlayerRingRightFG(iconName, color);
                instance.SetPlayerRingRightBG(qulityIcon);
                instance.SetPlayerRingRightName(name);
                break;
            case EquipSlot.Shoes:
                instance.SetPlayerShoesFG(iconName, color);
                instance.SetPlayerShoesBG(qulityIcon);
                instance.SetPlayerShoesName(name);
                break;
            case EquipSlot.Shoulder:
                instance.SetPlayerShoudersFG(iconName, color);
                instance.SetPlayerShoudersBG(qulityIcon);
                instance.SetPlayerShoudersName(name);
                break;
            case EquipSlot.Weapon:
                instance.SetPlayerWeaponFG(iconName, color);
                instance.SetPlayerWeaponBG(qulityIcon);
                instance.SetPlayerWeaponName(name);
                break;

        }
    }

    private void OnPropUse()
    {
        Mogo.Util.LoggerHelper.Debug("OnPropUse!!!");
        MogoWorld.thePlayer.RpcCall("UseItemReq", m_selectedItem.id, m_selectedItem.gridIndex, 1);
    }

    private void OnDecompose()
    {
        decomposeManager.Decompose(m_selectedItem.id, m_selectedItem.gridIndex);

    }

    private void OnMaterialTipSell()
    {
        LoggerHelper.Debug("OnMaterialTipSell");
        myself.RpcCall("SellForItems", m_selectedItem.id, m_selectedItem.gridIndex, ((ItemMaterial)m_selectedItem).templateId, m_selectedItem.stack);
    }

    private void OnMaterialTipEnhance()
    {
        Mogo.Util.LoggerHelper.Debug("OnMaterialTipEnhance");
        MenuUIViewManager.Instance.ShowMaterialTip(false);
        MogoUIManager.Instance.SwitchStrenthUI(
           delegate()
           {
               int index = 0;
               StrenthenUIViewManager.Instance.SetCurrentDownGrid(index);
               EventDispatcher.TriggerEvent<int>(BodyEnhanceManager.ON_SELECT_SLOT, index);
               EquipTipManager.Instance.CloseEquipTip();
           });


    }

    private void OnMaterialTipClose()
    {
        MenuUIViewManager.Instance.ShowMaterialTip(false);
    }

    private void OnPropTipClose()
    {
        MenuUIViewManager.Instance.ShowPropTip(false);
    }

    private void OnCompose()
    {
        ItemJewel jewel = (ItemJewel)m_selectedItem;
        composeManager.Compose(jewel.subtype, jewel.level);
    }

    private void InsetJewel()
    {
        CurrentView = View.InsetView;
        m_currentEquipmentView = View.InsetView;
        EquipTipManager.Instance.CloseEquipTip();
        insetManager.InsetJewelInBag(m_selectedItem.gridIndex);
    }

    private void OnPackageSwitch(int index)
    {
        switch ((PackageUITab)CurrentTagIndex)
        {
            case PackageUITab.EquipmentTab:
                m_pageIndexEquipment = MenuUIViewManager.Instance.GetCurrentPage();
                break;
            case PackageUITab.JewelTab:
                m_pageIndexJewel = MenuUIViewManager.Instance.GetCurrentPage();
                break;
            case PackageUITab.MaterialTab:
                m_pageIndexMaterial = MenuUIViewManager.Instance.GetCurrentPage();
                break;

        }
        CurrentTagIndex = index;
        RefreshPackageUI();
        if (m_isNeedAutoSortBag) SortTheBag();
    }

    public void RemoveListeners()
    {

        EventDispatcher.RemoveEventListener(ON_INVENTORY_SORT, SortTheBag);
        EventDispatcher.RemoveEventListener<int>(ON_ITEMS_SELECTED, OnSelectItemInbag);
        EventDispatcher.RemoveEventListener(ON_EQUIP, OnEquip);
        EventDispatcher.RemoveEventListener(ON_REMOVE_EQUIP, OnRemoveEquip);
        EventDispatcher.RemoveEventListener(ON_INVENTORY_SHOW, OnInventoryShow);
        EventDispatcher.RemoveEventListener<int>(ON_EQUIP_GRID_UP, OnEquipGridUp);
        EventDispatcher.RemoveEventListener(ON_STRENTHEN_EQUIP, StrengthEquip);
        EventDispatcher.RemoveEventListener(ON_EQUIP_INSET, EquipToInsetJewel);
        EventDispatcher.RemoveEventListener<int>(ON_PACKAGE_SWITCH, OnPackageSwitch);
        EventDispatcher.RemoveEventListener(ON_EQUIP_SHOW, ShowItem);
        EventDispatcher.RemoveEventListener(ON_INSET_JEWEL, InsetJewel);
        EventDispatcher.RemoveEventListener(ON_COMPOSE, OnCompose);

        EventDispatcher.RemoveEventListener(InventoryManager.ON_EQUIP_COMSUME_SHOW, OnEquipComsumeShow);
        EventDispatcher.RemoveEventListener(InventoryManager.ON_DECOMPOSE, OnDecompose);



        EventDispatcher.RemoveEventListener(MenuUIDict.MenuUIEvent.MATERIL_TIP_CLOSE, OnMaterialTipClose);
        EventDispatcher.RemoveEventListener(MenuUIDict.MenuUIEvent.MATERIL_TIP_ENHANCE, OnMaterialTipEnhance);
        EventDispatcher.RemoveEventListener(MenuUIDict.MenuUIEvent.MATERIL_TIP_SELL, OnMaterialTipSell);

        EventDispatcher.RemoveEventListener(MenuUIDict.MenuUIEvent.PROP_TIP_CLOSE, OnPropTipClose);
        EventDispatcher.RemoveEventListener(MenuUIDict.MenuUIEvent.PROP_TIP_USE, OnPropUse);
        EventDispatcher.RemoveEventListener(MenuUIDict.MenuUIEvent.PROP_TIP_SELL, OnMaterialTipSell);

        EventDispatcher.RemoveEventListener<int>(InventoryEvent.ShowTip, ShowItemTip);

        insetManager.RemoveListener();
        composeManager.RemoveListener();
        decomposeManager.RemoveListener();
        equipUpgradeManager.RemoveListener();

        m_itemsInBag.Clear();
        m_itemsOnEquip.Clear();
    }

    private void AddListeners()
    {

        EventDispatcher.AddEventListener(ON_INVENTORY_SORT, SortTheBag);
        EventDispatcher.AddEventListener<int>(ON_ITEMS_SELECTED, OnSelectItemInbag);
        EventDispatcher.AddEventListener(ON_EQUIP, OnEquip);
        EventDispatcher.AddEventListener(ON_REMOVE_EQUIP, OnRemoveEquip);
        EventDispatcher.AddEventListener(ON_INVENTORY_SHOW, OnInventoryShow);
        EventDispatcher.AddEventListener<int>(ON_EQUIP_GRID_UP, OnEquipGridUp);
        EventDispatcher.AddEventListener(ON_STRENTHEN_EQUIP, StrengthEquip);
        EventDispatcher.AddEventListener(ON_EQUIP_INSET, EquipToInsetJewel);
        EventDispatcher.AddEventListener<int>(ON_PACKAGE_SWITCH, OnPackageSwitch);
        EventDispatcher.AddEventListener(ON_EQUIP_SHOW, ShowItem);
        EventDispatcher.AddEventListener(ON_INSET_JEWEL, InsetJewel);
        EventDispatcher.AddEventListener(ON_COMPOSE, OnCompose);

        EventDispatcher.AddEventListener(InventoryManager.ON_EQUIP_COMSUME_SHOW, OnEquipComsumeShow);
        EventDispatcher.AddEventListener(InventoryManager.ON_DECOMPOSE, OnDecompose);


        EventDispatcher.AddEventListener(MenuUIDict.MenuUIEvent.MATERIL_TIP_CLOSE, OnMaterialTipClose);
        EventDispatcher.AddEventListener(MenuUIDict.MenuUIEvent.MATERIL_TIP_ENHANCE, OnMaterialTipEnhance);
        EventDispatcher.AddEventListener(MenuUIDict.MenuUIEvent.MATERIL_TIP_SELL, OnMaterialTipSell);

        EventDispatcher.AddEventListener(MenuUIDict.MenuUIEvent.PROP_TIP_CLOSE, OnPropTipClose);
        EventDispatcher.AddEventListener(MenuUIDict.MenuUIEvent.PROP_TIP_USE, OnPropUse);
        EventDispatcher.AddEventListener(MenuUIDict.MenuUIEvent.PROP_TIP_SELL, OnMaterialTipSell);

        EventDispatcher.AddEventListener<int>(InventoryEvent.ShowTip, ShowItemTip);

        //EventDispatcher.AddEventListener(USE_JEWEL, UseJewel);
        //EventDispatcher.AddEventListener<int, int>(InventoryEvent.UseItems, UseItems);
        //EventDispatcher.AddEventListener<int, int>(InventoryEvent.DelItems, DelItems);
        //EventDispatcher.AddEventListener(InventoryEvent.DelAllItem, DelAllItem);
        //EventDispatcher.AddEventListener<int, int>(InventoryEvent.SaleItems, SaleItems);
        //EventDispatcher.AddEventListener<int>(InventoryEvent.ShowItem, ShowItem);
        //EventDispatcher.AddEventListener<int>(InventoryEvent.StrengthItem, StrengthItem);

        //EventDispatcher.AddEventListener<int>(InventoryEvent.SplitItem, SplitItem);
        //EventDispatcher.AddEventListener(InventoryEvent.UpdateItemGrid, UpdateItemGrid);
        //EventDispatcher.AddEventListener(InventoryEvent.UpdateItemAllGrid, UpdateItemAllGrid);

    }

    private void OnEquipComsumeShow()
    {
        //Debug.LogError("OnEquipComsumeShow");
        //m_currentEquipmentView = View.BodyEnhanceView;
        //CurrentView = m_currentEquipmentView;
        RefreshUI();
    }

    private void OnInventoryShow()
    {
        CurrentView = m_currentPackageView;
        CurrentTagIndex = 1;
        Mogo.Util.LoggerHelper.Debug("OnInventoryShow");
        RefreshUI();
        if (CurrentView == View.PackageView && m_isNeedAutoSortBag)
        {
            SortTheBag();
        }
    }

    private void DelItems(int idx, int num)
    {
    }

    private void DelAllItem()
    {
    }

    private void SaleItems(int idx, int num)
    {
        //todo能否出售判断
    }

    private void ShowItem()
    {
        MogoUIManager.Instance.ShowMogoCommuntiyUI();
        MogoUIManager.Instance.SwitchWorldChannelUI();
        //MenuUIViewManager.Instance.ShowPackageDetailInfo(false);
        EquipTipManager.Instance.CloseEquipTip();

        EventDispatcher.TriggerEvent<int, List<int>>("EquipmentShowupUp", m_selectedItem.templateId, ((ItemEquipment)m_selectedItem).jewelSlots);
    }

    private void OnSwitchInsetUIDone()
    {
        //Debug.LogError("OnSwitchInsetUIDone");
        m_currentEquipmentView = View.InsetView;
        CurrentView = View.InsetView;
        int index = ((ItemEquipment)m_selectedItem).gridIndex;

        insetManager.CurrentSlot = index + 1;
        Debug.LogError("  insetManager.CurrentSlot:" + insetManager.CurrentSlot);
        //if (index == 10)
        //{
        //    InsetUIViewManager.Instance.SetCurrentDownGrid(0);
        //    EventDispatcher.TriggerEvent<int>(InsetManager.ON_EQUIP_SELECT, 0);
        //}
        //else
        //{
        //    InsetUIViewManager.Instance.SetCurrentDownGrid(index + 1);
        //    EventDispatcher.TriggerEvent<int>(InsetManager.ON_EQUIP_SELECT, index + 1);
        //}


        insetManager.RefreshUI();
    }
    /// <summary>
    /// 在装备tip中点击镶嵌后
    /// </summary>
    private void EquipToInsetJewel()
    {
        EquipTipManager.Instance.CloseEquipTip();

        int index = ((ItemEquipment)m_selectedItem).gridIndex;

        index = index + 1;
        //Debug.LogError("index:" + index);
        InsetManager.Instance.SwitchToInsetUI(index);
        // MogoUIManager.Instance.SwitchInsetUI(OnSwitchInsetUIDone);
    }

    private void OnStrengthEquipUIDone()
    {

        //Debug.LogError("OnStrengthEquipUIDone!");
        Mogo.Util.LoggerHelper.Debug(m_selectedItem.gridIndex + "");
        int index = ((ItemEquipment)m_selectedItem).gridIndex;
        //Debug.LogError(index + "");
        index++;
        if (index >= (int)EquipSlot.RightRing)
        {
            index--;
        }
        BodyEnhanceManager.Instance.CurrentSlot = index;
        BodyEnhanceManager.Instance.RefreshUI();
        //EventDispatcher.TriggerEvent<int>(BodyEnhanceManager.ON_SELECT_SLOT, index);
    }

    private void StrengthEquip()
    {//todo打开强化界面

        InventoryManager.Instance.CurrentView = InventoryManager.View.BodyEnhanceView;
        EquipTipManager.Instance.CloseEquipTip();
        //Debug.LogError("SwitchStrenthUI");
        MogoUIManager.Instance.SwitchStrenthUI(OnStrengthEquipUIDone);//

    }

    private void SplitItem(int idx)
    {

    }

    /// <summary>
    /// 请求整理背包
    /// </summary>
    /// <param name="pageIndex"></param>
    public void SortTheBag()
    {
        //List<ItemParent> list = new List<ItemParent>();
        //int start = pageIndex * GRID_COUNT_PER_PAGE;
        //int end = start + GRID_COUNT_PER_PAGE;//Exculde
        //foreach (ItemParent item in m_itemsInBag[m_currentTagIndex].Values)
        //{
        //    if (item.gridIndex < start || item.gridIndex >= end) continue;
        //    list.Add(item);
        //}
        //if (list.Count <= 0) return;
        LoggerHelper.Debug("SortTheBag");
        myself.RpcCall("TidyInventory");
    }

    ItemEquipment GetItemEquipmentByInstance(ItemInstance instance)
    {
        ItemEquipmentInstance equipInstance = new ItemEquipmentInstance()
        {
            id = instance.id,
            bagType = instance.bagType,
            bindingType = instance.bindingType,
            stack = instance.stack,
            templeId = instance.templeId,
            gridIndex = instance.gridIndex,
            sourceKey = instance.sourceKey,
            sourceValue = instance.sourceValue,
        };
        if (instance.extendInfo != null)
        {
            //Mogo.Util.LoggerHelper.Debug("instance.extendInfo:" + instance.extendInfo);
            if (instance.extendInfo.ContainsKey(0))
            {
                //Mogo.Util.LoggerHelper.Debug("instance.extendInfo:" + instance.extendInfo["lock"]);
                equipInstance.locked = (instance.extendInfo[0] == 0 ? false : true);
            }

            if (instance.extendInfo.ContainsKey(1))
            {
                //Mogo.Util.LoggerHelper.Debug("instance.extendInfo:" + instance.extendInfo["lock"]);
                equipInstance.isActive = (instance.extendInfo[1] == 0 ? false : true);
            }
        }


        //slot表示格式转换，dic->list
        equipInstance.jewelSlots = new List<int>();
        LoggerHelper.Debug("instance.templeId:" + instance.templeId);
        ItemEquipmentData equip = ItemEquipmentData.dataMap[instance.templeId];
        if (equip.jewelSlot != null && equip.jewelSlot.Count > 0)
        {
            if (equip.jewelSlot[0] != -1)
            {
                for (int i = 0; i < equip.jewelSlot.Count; i++)
                {
                    equipInstance.jewelSlots.Add(-1);
                }
            }
        }

        if (instance.jewelSlots != null)
        {
            foreach (KeyValuePair<int, int> pair in instance.jewelSlots)
            {
                equipInstance.jewelSlots[pair.Key - 1] = pair.Value;
            }
        }



        return new ItemEquipment(equipInstance);
    }

    ItemParent AddItem(ItemInstance instance)
    {
        ItemParent item = null;
        LoggerHelper.Debug("AddItem:" + instance.bagType + ",index:" + instance.gridIndex);
        switch (instance.bagType)
        {
            case ITEM_TYPE_EQUIPMENT:
                if (ItemEquipmentData.dataMap.ContainsKey(instance.templeId))
                {
                    item = GetItemEquipmentByInstance(instance);
                }
                else
                {
                    ItemParentInstance toolInstance = new ItemParentInstance()
                    {
                        id = instance.id,
                        bagType = instance.bagType,
                        stack = instance.stack,
                        templeId = instance.templeId,
                        gridIndex = instance.gridIndex,
                    };
                    item = new ItemMaterial(toolInstance);
                }

                m_itemsInBag[ITEM_TYPE_EQUIPMENT][item.gridIndex] = item;
                break;

            case ITEM_TYPE_JEWEL:
                ItemJewelInstance jewelInstance = new ItemJewelInstance()
                {
                    id = instance.id,
                    bagType = instance.bagType,
                    bindingType = instance.bindingType,
                    stack = instance.stack,
                    templeId = instance.templeId,
                    gridIndex = instance.gridIndex,
                };
                item = new ItemJewel(jewelInstance);
                m_itemsInBag[ITEM_TYPE_JEWEL][item.gridIndex] = item;
                break;
            case ITEM_TYPE_ONEQUIP:
                item = GetItemEquipmentByInstance(instance);
                //Mogo.Util.LoggerHelper.Debug("item.gridIndex:" + item.gridIndex + "!!!!!!!!!!!");
                m_itemsOnEquip[item.gridIndex + 1] = (ItemEquipment)item;
                //Mogo.Util.LoggerHelper.Debug("inventroy equip:" + item.templateId);
                myself.Equip(item.templateId);
                break;
            case ITEM_TYPE_MATERIAL:
                ItemParentInstance materialInstance = new ItemParentInstance()
                {
                    id = instance.id,
                    bagType = instance.bagType,
                    stack = instance.stack,
                    templeId = instance.templeId,
                    gridIndex = instance.gridIndex,
                };
                item = new ItemMaterial(materialInstance);
                m_itemsInBag[ITEM_TYPE_MATERIAL][item.gridIndex] = item;
                break;

        }
        return item;
    }

    public void UpdateItemGrid(byte updateType, LuaTable _info)
    {
        Mogo.Util.LoggerHelper.Debug("UpdateItemGrid");
        object obj;
        if (updateType == 1)
        {
            Mogo.Util.LoggerHelper.Debug("UpdateItemGrid:Delete!!!!");
            //删除
            Utils.ParseLuaTable(_info, typeof(List<int>), out obj);
            List<int> temp = obj as List<int>;
            if (temp[0] >= 1 && temp[0] <= 3)
            {
                m_itemsInBag[temp[0]].Remove(temp[1]);
                Mogo.Util.LoggerHelper.Debug("delete bag:" + temp[0] + ",index:" + temp[1]);
            }
            else if (temp[0] == ITEM_TYPE_ONEQUIP)
            {
                Mogo.Util.LoggerHelper.Debug("delete m_itemsOnEquip:" + temp[0] + ",index:" + temp[1]);
                ItemEquipment tempEquip = m_itemsOnEquip[temp[1] + 1];
                m_itemsOnEquip.Remove(temp[1] + 1);
                myself.RemoveEquip(tempEquip.templateId);

            }
        }
        else
        {
            Mogo.Util.LoggerHelper.Debug("UpdateItemGrid:Update!!!!");
            //更新
            Utils.ParseLuaTable(_info, typeof(ItemInstance), out obj);

            ItemInstance instance = obj as ItemInstance;

            //得到物品
            if (updateType == 3)
            {
                LoggerHelper.Debug("get_item " + instance.templeId);
                GuideSystem.Instance.TriggerEvent<int>(GlobalEvents.GetItem, instance.templeId);

                ItemParentData item = ItemParentData.GetItem(instance.templeId);
                int num;
                Mogo.Util.LoggerHelper.Debug("instance.bagType:" + instance.bagType);
                if (m_itemsInBag[instance.bagType].ContainsKey(instance.gridIndex))
                {
                    Mogo.Util.LoggerHelper.Debug("instance.stack :" + instance.stack);
                    Mogo.Util.LoggerHelper.Debug("m_itemsInBag[instance.bagType][instance.gridIndex].stack :" + m_itemsInBag[instance.bagType][instance.gridIndex].stack);
                    num = instance.stack - m_itemsInBag[instance.bagType][instance.gridIndex].stack;
                    Mogo.Util.LoggerHelper.Debug("num:" + num);
                }
                else
                {
                    num = instance.stack;
                    Mogo.Util.LoggerHelper.Debug("num:" + num);
                }
                Mogo.Util.LoggerHelper.Debug("you get\"" + item.Name + "\"x" + num);

                if (!m_isCaching)
                {
                    m_isCaching = true;
                    TimerHeap.AddTimer(500, 0, DoShowGetSth);
                }
                if (m_getSthDic.ContainsKey(item.Name))
                {
                    m_getSthDic[item.Name] += num;
                }
                else
                {
                    m_getSthDic[item.Name] = num;
                }

            }
            ItemParent itemTemp = AddItem(instance);
            if (updateType == 3 && itemTemp.itemType == 1)
            {
                TipManager.Instance.OnGetEquipment(itemTemp as ItemEquipment);
            }
        }

        m_versionId++;
        RefreshUI();


    }

    private void DoShowGetSth()
    {
        foreach (KeyValuePair<string, int> pair in m_getSthDic)
        {
            MogoMsgBox.Instance.ShowFloatingTextQueue(LanguageData.GetContent(898, pair.Key, pair.Value));
        }
        m_getSthDic.Clear();
        m_isCaching = false;
        //new GetItem("您获得了" + item.Name + "x" + num);

    }

    public void UpdateItemAllGrid(LuaTable info)
    {
        Mogo.Util.LoggerHelper.Debug("UpdateItemAllGrid");
        m_versionId++;
        Mogo.Util.LoggerHelper.Debug("UpdateItemAllGrid start!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        try
        {
            //LoggerHelper.Debug(Mogo.RPC.Utils.PackLuaTable(info));
            List<ItemInstance> temp = new List<ItemInstance>();


            if (!Utils.ParseLuaTable(info, out temp))
            {
                LoggerHelper.Error(Mogo.RPC.Utils.PackLuaTable(info));
                return;
            }

            switch (temp.Get(0).bagType)
            {
                case ITEM_TYPE_EQUIPMENT:
                    m_itemsInBag[ITEM_TYPE_EQUIPMENT].Clear();
                    break;

                case ITEM_TYPE_JEWEL:
                    m_itemsInBag[ITEM_TYPE_JEWEL].Clear();
                    break;
                case ITEM_TYPE_MATERIAL:
                    m_itemsInBag[ITEM_TYPE_MATERIAL].Clear();
                    break;
                case ITEM_TYPE_ONEQUIP:
                    m_itemsOnEquip.Clear();
                    break;
            }
            foreach (ItemInstance instance in temp)
            {
                AddItem(instance);
            }
        }
        catch (Exception e)
        {
            LoggerHelper.Error(Mogo.RPC.Utils.PackLuaTable(info));
            LoggerHelper.Error(e);
        }


        RefreshUI();
    }

    private void OnExchangeEquip(ItemParent item)
    {
        EquipTipManager.Instance.CloseEquipTip();
        EquipExchangeManager.Instance.ShowUI(item.level, item.subtype);
    }

    private bool CanMaterialExchangeEquip(ItemParent item)
    {
        return item.type == 9;
    }

    private void OnUpGragdeEquip()
    {
        Mogo.Util.LoggerHelper.Debug("OnUpGragdeEquip");
        //MogoMsgBox.Instance.ShowFloatingText("开发中...");
        EquipUpgradeManager.Instance.Equip = m_selectedItem as ItemEquipment;
        EquipUpgradeManager.Instance.ShowUI();
        EquipTipManager.Instance.CloseEquipTip();

    }

    //private void OnGetItem()
    //{
    //    //todo 当获得物体后（指非背包操作获得的物品，其他）更新背包数据和界面显示
    //}
    //private void OnItemChange()
    //{
    //}
    //private int GetLevel(int id)
    //{
    //    int lv = 0;
    //    lv = id % 100000;
    //    lv = (int)(lv / 1000);
    //    return lv;
    //}

    //private int GetItemType(int id)
    //{
    //    int t = 1;
    //    t = (int)(id / 1000000);
    //    t = t % 10;
    //    return t;
    //}

    //private int GetItemSubType(int id)
    //{
    //    int t = 1;
    //    t = id % 1000000;
    //    t = (int)(t / 10000);
    //    return t;
    //}

    //private int GetItemQuality(int id)
    //{
    //    int q = 1;
    //    q = id % 1000;
    //    q = (int)(q / 100);
    //    return q;
    //}

    //private void UseItems(int idx, int num)
    //{
    //    ItemParent item = m_itemsInBag[m_currentTagIndex][idx];
    //    int itemType = item.type;
    //    if (!Enum.IsDefined(typeof(ItemType), itemType))
    //    {
    //        LoggerHelper.Debug("item type not exist");
    //        return;
    //    }
    //    switch ((ItemType)itemType)
    //    {
    //        case ItemType.ARMOR:
    //        case ItemType.WEAPON:
    //            {
    //                _UseEquips(item, idx, num);
    //                break;
    //            }
    //        case ItemType.ITEM:
    //            {
    //                _UseItems(item, idx, num);
    //                break;
    //            }
    //        case ItemType.JEWEL:
    //            {
    //                break;
    //            }
    //        case ItemType.RUNE:
    //            {
    //                break;
    //            }
    //        case ItemType.SPIRIT:
    //            {
    //                break;
    //            }
    //    }
    //}

    //private void _UseItems(ItemParent item, int idx, int num)
    //{
    //    //int limitedLv = GetLevel(item.templateId);
    //    //ItemOthersData baseData = (ItemOthersData)item.BaseData;
    //    //ItemOthers _item = (ItemOthers)item;
    //    //if (baseData.useTimes == -1)
    //    //{
    //    //    LoggerHelper.Debug("the item's useTimes == -1, can't be use");
    //    //    return;
    //    //}
    //    //if (baseData.useLevel > MogoWorld.thePlayer.level)
    //    //{
    //    //    LoggerHelper.Debug("the player's level too small");
    //    //    return;
    //    //}
    //    //if (_item.ColdTimeLeft > 0)
    //    //{
    //    //    LoggerHelper.Debug("the itme is colding");
    //    //    return;
    //    //}
    //}

    //private void UseJewel(ItemParent item, int idx, int num)
    //{

    //}

    //private void _UseEquips(ItemParent item, int idx, int num)
    //{

    //}

    //private void _UseRunes(ItemParent item, int idx, int num)
    //{
    //}

    //private void _UseSpirits(ItemParent item, int idx, int num)
    //{
    //}

    ///// <summary>
    ///// 当背包整理好后
    ///// </summary>
    ///// <param name="list"></param>
    ///// <param name="tagIndex"></param>
    ///// <param name="pageIndex"></param>
    //private void OnBagSorted(List<ItemParent> list, int tagIndex, int pageIndex)
    //{
    //    int start = pageIndex * GRID_COUNT_PER_PAGE;
    //    int end = start + GRID_COUNT_PER_PAGE;//Exculde

    //    //清除所整理的页
    //    for (int i = start; i < end; i++)
    //    {
    //        m_itemsInBag[tagIndex][i] = null;
    //    }

    //    //重新赋值
    //    foreach (ItemParent item in list)
    //    {
    //        m_itemsInBag[tagIndex].Add(item.gridIndex, item);
    //    }

    //    MenuUILogicManager.Instance.ResetInventoryItemsInPage(pageIndex, list);
    //}

    public void JewelCombineResp(byte subType, byte level, byte errorId)
    {
        composeManager.JewelCombineResp(subType, level, errorId);
    }

    public void JewelCombineInEqiResp(byte errorId)
    {
        composeManager.JewelCombineInEqiResp(errorId);
    }

    public void JewelCombineAnywayMoneyResp(uint money)
    {
        composeManager.JewelCombineAnywayMoneyResp(money);
    }

    public void JewelCombineAnywayResp(byte errorId)
    {
        composeManager.JewelCombineAnywayResp(errorId);
    }

    public void JewelInlayIntoEqiResp(byte errorId)
    {
        insetManager.JewelInlayIntoEqiResp(errorId);
    }

    public void JewelInlayResp(byte errorId)
    {
        LoggerHelper.Debug("JewelInlayResp:" + errorId);
        InsetUIViewManager.Instance.ShowInsetDialogDiamondTip(false);
        InsetManager.ShowInfoByErrorId(errorId);
        insetManager.RefreshUI();

    }

    public void JewelOutlayResp(byte errorId)
    {
        insetManager.JewelOutlayResp(errorId);
    }

    public void JewelSellResp(int gold)
    {
        LoggerHelper.Debug("JewelSellResp:" + gold);
    }

    #region 背包回调

    /// <summary>
    /// RespsForChgAndRmEquip
    /// </summary>
    /// <param name="id"></param>
    /// <param name="errorCode">
    /// </param>
    public void RespsForChgAndRmEquip(int id, byte errorCode)
    {
        Mogo.Util.LoggerHelper.Debug("RespsForChgAndRmEquip:" + errorCode);
        ShowEquipError(errorCode);
        EquipTipManager.Instance.CloseEquipTip();

        //if (hasJewelLayout == 1)
        //{
            //switch (errorCode)
            //{
            //    case 0: break;
            //    case 1: break;
            //    case 2: break;
            //}
            //TipUIManager.Instance.HideAll(TipManager.TIP_TYPE_JEWEL_LAYOUT);
            //TipViewData viewData = new TipViewData();
            //viewData.itemId = 1412061;
            //viewData.tipText = LanguageData.GetContent(3041);
            //viewData.btnName = LanguageData.GetContent(3042);
            //viewData.btnAction = () =>
            //{
            //    ItemParentData equip = ItemParentData.GetItem(id);
            //    //insetManager.CurrentSlot = 10;

            //    insetManager.SwitchToInsetUI(slot);
            //    TipUIManager.Instance.HideAll(TipManager.TIP_TYPE_JEWEL_LAYOUT);
            //};
            //viewData.priority = TipManager.TIP_TYPE_JEWEL_LAYOUT;
            //TipUIManager.Instance.AddTipViewData(viewData);
        //}


        //if (m_selectedItem == null)
        //{
        //    //myself.Equip(id);
        //    return;
        //}
        //if (m_selectedItem.bagType == ITEM_TYPE_EQUIPMENT)
        //{
        //    //MenuUIViewManager.Instance.ShowPackageDetailInfo(false);
        //    EquipTipManager.Instance.CloseEquipTip();
        //    //MenuUIViewManager.Instance.ShowPacakgeCurrentDetailInfo(false);
        //    ShowEquipError(errorCode);
        //    //myself.Equip(id);
        //}
        //else
        //{
        //    //MenuUIViewManager.Instance.ShowPackageDetailInfo(false);
        //    EquipTipManager.Instance.CloseEquipTip();
        //    ShowRemoveEquipError(errorCode);
        //    //myself.RemoveEquip(id);
        //}
    }
    #endregion


    /// <summary>
    /// </summary>
    /// <param name="errorCode">
    ///SUCCESS = 0, --卸装成功
    ///EQUIPMENT_NOT_EXISTED = 1, --背包中不存在该装备
    ///DATA_UNMATCH = 2, --前后端数据不匹配
    ///EQUIPMENT_INVENTORY_FULL = 3, --背包已满
    ///UNKNOW = 4, --未知错误
    /// </param>
    private void ShowRemoveEquipError(byte errorCode)
    {
        string msg = "";
        int index = 490;
        switch (errorCode)
        {
            case 0: msg = LanguageData.dataMap[index].content; break;
            case 1: msg = LanguageData.dataMap[index + 1].content; break;
            case 2: msg = LanguageData.dataMap[index + 2].content; break;
            case 3: msg = LanguageData.dataMap[index + 3].content; break;
            case 4: msg = LanguageData.dataMap[index + 4].content; break;
        }

        MogoMsgBox.Instance.ShowMsgBox(msg);
        //MogoGlobleUIManager.Instance.Confirm(msg, (rst) => { MogoGlobleUIManager.Instance.ConfirmHide(); });
        return;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="errorCode">
    ///SUCCESS = 0, 
    ///EQUIPMENT_NOT_EXISTED = 1, //装备不存在
    ///EQUIPMENT_INVENTORY_FULL = 2, //背包满了
    ///EQUIPMENT_NOT_EXISTED_IN_CFG_TBL = 3,//系统不存在这个装备
    ///EQUIPMENT_NOT_EXISTED_IN_ATTRI_TBL = 4,//系统属性表检索不存在
    ///LEVEL_NOT_ENOUGH = 5,
    ///DATA_UNMATCH = 6,
    ///VOCATION_UNMATCH = 7,
    ///NOT_KNOWN = 8,
    /// </param>
    private void ShowEquipError(byte errorCode)
    {
        string msg = "";
        int index = 401;
        switch (errorCode)
        {
            case 0:
                MogoMsgBox.Instance.ShowFloatingTextQueue(LanguageData.GetContent(47280));
                return;
            default:
                msg = LanguageData.dataMap[index + errorCode].content;
                break;
        }
        MogoMsgBox.Instance.ShowMsgBox(msg);

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="errorCode">
    ///SUCCESS = 0, --出售成功
    ///ITEM_NOT_EXISTED = 1, --道具系统中不存在
    ///COUNT_ERROR = 2, --数量错误
    ///FORBID_SELL = 3, --不可售卖
    /// DATA_UNMATCH = 4, --前后端数据不匹配
    ///UNKNOW = 5, --位置错误
    /// </param>
    public void SellForResp(byte errorCode)
    {
        //MenuUIViewManager.Instance.ShowMaterialTip(false);
        //MenuUIViewManager.Instance.ShowPropTip(false);
        EquipTipManager.Instance.CloseEquipTip();
        string msg = "";
        int index = 501;
        switch (errorCode)
        {
            case 0:
                msg = LanguageData.dataMap[index].content;
                break;
            case 1:
                msg = LanguageData.dataMap[index + 1].content;
                break;
            case 2:
                msg = LanguageData.dataMap[index + 2].content;
                break;
            case 3:
                msg = LanguageData.dataMap[index + 3].content;
                break;
            case 4:
                msg = LanguageData.dataMap[index + 4].content;
                break;
            case 5:
                msg = LanguageData.dataMap[index + 5].content;
                break;
        }
        //MogoGlobleUIManager.Instance.Confirm(msg, (rst) => { MogoGlobleUIManager.Instance.ConfirmHide(); });
        MogoMsgBox.Instance.ShowMsgBox(msg);

    }

    /// <summary>
    /// 得到材料数量
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public int GetMaterialNum(int id)
    {
        int count = 0;
        foreach (ItemParent item in m_itemsInBag[ITEM_TYPE_MATERIAL].Values)
        {
            if (item.templateId != id) continue;
            count += item.stack;
        }
        return count;
    }

    /// <summary>
    /// 得到材料数量,同类型且级别高于也算
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public int GetMaterialAllNum(int id)
    {
        int count = 0;
        ItemMaterialData material = ItemMaterialData.dataMap[id];
        foreach (ItemParent item in m_itemsInBag[ITEM_TYPE_MATERIAL].Values)
        {
            if ((item.templateId != id && item.subtype != material.subtype)
                || material.level > ((ItemMaterial)item).level
                || material.type != item.type) continue;

            count += item.stack;
        }
        return count;
    }

    /// <summary>
    /// 得到所需材料列表 list<材料id，数量>,从quality低到高排列
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public List<KeyValuePair<int, int>> GetMaterialList(int id, int needNum)
    {
        if (GetMaterialAllNum(id) < needNum) return null;

        ItemMaterialData material = ItemMaterialData.dataMap[id];
        List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>();
        int count = needNum;
        int level = material.level;
        while (count > 0)
        {
            int num = 0;
            int tempId = -1;
            foreach (ItemParent item in m_itemsInBag[ITEM_TYPE_MATERIAL].Values)
            {
                if ((item.templateId != id && item.subtype != material.subtype)
                    || level != ((ItemMaterial)item).level
                    || item.type != material.type) continue;

                tempId = item.templateId;
                num += item.stack;
                if (count > num)
                {
                    count -= num;
                }
                else
                {
                    num = count;
                    list.Add(new KeyValuePair<int, int>(tempId, num));
                    return list;
                }
            }

            if (num > 0)
            {
                list.Add(new KeyValuePair<int, int>(tempId, num));
            }
            Mogo.Util.LoggerHelper.Debug("count:" + count + ",level:" + level);
            level++;
        }

        return list;
    }

    /// <summary>
    /// ERR_USEITEM_SUCCESS                = 0,          --使用成功
    /// ERR_USEITEM_IDX_ERROR              = 1,          --道具索引错误
    /// ERR_USEITEM_ID_ERROR               = 2,          --道具id错误
    /// ERR_USEITEM_CFG_ERROR              = 3,          --配置错误
    /// ERR_USEITEM_FORBID_USE             = 4,          --不可使用
    /// ERR_USEITEM_COUNT_ERROR            = 5,          --数量错误
    /// ERR_USEITEM_COLD_LIMIT             = 6,          --冷却未结束
    /// ERR_USEITEM_VIP_LEVEL_LIMIT        = 7,          --VIP等级受限
    /// ERR_USEITEM_VOCATION_LIMIT         = 8,          --职业受限
    /// ERR_USEITEM_USELEVEL_LIMIT         = 9,          --使用等级受限
    /// ERR_USEITEM_SPACE_UNENOUGH         = 10,         --空间不足
    /// ERR_USEITEM_VIP_UNEFFECT           = 11,         --VIP效果不足
    /// ERR_USERITEM_COST_UNENOUGH         = 12,         --消耗不足
    /// 
    /// AVATAR_PROP_DIAMOND     = 1, --钻石
    /// AVATAR_PROP_GOLD        = 2, --金币
    /// AVATAR_PROP_EXP         = 3, --经验
    /// AVATAR_PROP_VIP         = 4, --vip卡
    /// AVATAR_PROP_ESCUBE      = 5, --特殊宝箱
    /// AVATAR_PROP_ENERGY      = 6, --体力
    /// AVATAR_PROP_BUFF        = 7, --buff
    /// </summary>
    /// <param name="id"></param>
    /// <param name="errorCode"></param>
    public void UseItemResp(int id, byte errorCode, LuaTable info)
    {
        //Debug.LogError("errorCode:" + errorCode);
        object obj;
        Utils.ParseLuaTable(info, typeof(Dictionary<int, int>), out obj);
        //<id,count>
        Dictionary<int, int> rewardDic = obj as Dictionary<int, int>;

        EquipTipManager.Instance.CloseEquipTip();
        EquipUpgradeViewManager.Instance.ShowMainUI(false);
        //MenuUIViewManager.Instance.ShowPropTip(false);
        int index = 2100;
        ItemParentData data = ItemParentData.GetItem(id);
        switch (errorCode)
        {
            //case 0:
            //m_propCdDic[item.cdTypes] = Time.time;
            //string msg = "你得到了:\n";
            //int count = rewardDic.Count;
            //int num = 0;
            //foreach (KeyValuePair<int, int> pair in rewardDic)
            //{
            //    Mogo.Util.LoggerHelper.Debug("key:" + pair.Key + ",value:" + pair.Value);
            //    switch (pair.Key)
            //    {
            //        case 1: msg += pair.Value + "钻石"; break;
            //        case 2: msg += pair.Value + "金币"; break;
            //        case 6: msg += pair.Value + "体力"; break;
            ////        case 7: count--; continue;
            //        default:
            //            //ItemMaterialData itemTemp = ItemMaterialData.dataMap[pair.Key];
            //            ItemParentData itemTemp = ItemParentData.GetItem(pair.Key);
            //            msg += pair.Value + "个" + itemTemp.Name;
            //            break;
            //    }

            //    count--;
            //    num++;
            //    if (count >= 1)
            //    {
            //        msg += "\n";
            //    }
            //}
            //if (num > 0)
            //{
            //    MogoMsgBox.Instance.ShowMsgBox(msg);
            ////}
            //break;
            case 0:
                m_propCdDic[data.cdTypes] = Time.time;
                //Mogo.Util.LoggerHelper.Debug("rewardDic:" + rewardDic.Count);
                //foreach (KeyValuePair<int, int> pair in rewardDic)
                //{
                //    string msg = string.Empty;
                //    Mogo.Util.LoggerHelper.Debug("key:" + pair.Key + ",value:" + pair.Value);
                //    ItemParentData itemTemp = ItemParentData.GetItem(pair.Key);
                //    Mogo.Util.LoggerHelper.Debug("itemTemp.name:" + itemTemp.name);
                //    Mogo.Util.LoggerHelper.Debug("itemTemp.itemType:" + itemTemp.itemType);
                //    if (itemTemp.itemType == 0)
                //        msg = itemTemp.Name + "x" + pair.Value;
                //    //switch (pair.Key)
                //    //{
                //    //    case 1: msg = "钻石x" + pair.Value; break;
                //    //    case 2: msg = "金币x" + pair.Value; break;
                //    //    case 6: msg = "体力x" + pair.Value; break;
                //    //    case 7: continue;
                //    //    default:
                //    //        break;
                //    //}

                //    if (!msg.Equals(string.Empty))
                //    {
                //        MogoMsgBox.Instance.ShowFloatingTextQueue("您得到了" + msg);
                //    }
                //}
                break;
            //case 2:
            //    float time = Time.time - m_propCdDic[item.cdTypes];
            //    MogoMsgBox.Instance.ShowMsgBox(LanguageData.dataMap[index + errorCode - 1].Format(item.name, time));
            //    break;
            case 12:
                foreach (KeyValuePair<int, int> pair in rewardDic)
                {
                    Mogo.Util.LoggerHelper.Debug("key:" + pair.Key + ",value:" + pair.Value);
                    ItemParentData itemTemp = ItemParentData.GetItem(pair.Key);
                    if (GetItemNumById(itemTemp.id) >= pair.Value) continue;
                    MogoMsgBox.Instance.ShowMsgBox(LanguageData.dataMap[index + errorCode].Format(itemTemp.Name, data.Name));
                }
                break;
            case 7:
                Mogo.Util.LoggerHelper.Debug("item.vipLevel:" + data.vipLevel);
                MogoMsgBox.Instance.ShowMsgBox(LanguageData.dataMap[index + errorCode].Format(data.vipLevel, data.Name));
                break;
            case 8:
                string vocation = LanguageData.dataMap[data.useVocation].content;
                MogoMsgBox.Instance.ShowMsgBox(LanguageData.dataMap[index + errorCode].Format(vocation, data.Name));

                break;
            case 9:
                Debug.LogError("9");
                MogoMsgBox.Instance.ShowMsgBox(LanguageData.dataMap[index + errorCode].Format(data.useLevel, data.Name));
                break;
            case 10:
                MogoMsgBox.Instance.ShowMsgBox(LanguageData.dataMap[index + errorCode].Format(data.Name));
                break;
            case 19:
                MogoMsgBox.Instance.ShowMsgBox(LanguageData.GetContent(26830));
                break;
            case 20:
                MogoMsgBox.Instance.ShowMsgBox(LanguageData.GetContent(26831));
                break;
            default:
                MogoMsgBox.Instance.ShowMsgBox(LanguageData.dataMap[index + errorCode].content);
                break;
        }

    }

    private int GetSuitPutonNum(int suitId)
    {
        int count = 0;
        foreach (ItemEquipment equip in m_itemsOnEquip.Values)
        {
            if (equip.suitId == suitId && equip.isActive)
            {
                count++;
            }
        }
        return count;
    }

    public void ShowItemsGet(int theItem, int theNum = 1)
    {
        StringBuilder sb = new StringBuilder();

        var tempData = ItemParentData.GetItem(theItem);
        if (tempData != null)
        {
            sb.Append(tempData.Name);
            sb.Append("*");
            sb.Append(theNum);
        }

        if (sb.Length != 0)
            MogoMsgBox.Instance.ShowMsgBox(string.Format(LanguageData.GetContent(48409), sb)); // "你获得了：" + sb.ToString() + "。"
    }

    public void ShowItemsGet(Dictionary<int, int> items)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var item in items)
        {
            var tempData = ItemParentData.GetItem(item.Key);
            if (tempData != null)
            {
                sb.Append(tempData.Name);
                sb.Append("*");
                sb.Append(item.Value);
            }
        }
        if (sb.Length != 0)
            MogoMsgBox.Instance.ShowMsgBox(string.Format(LanguageData.GetContent(48409), sb)); // "你获得了：" + sb.ToString() + "。"
    }

    public void ShowItemsGet(List<int> items, List<int> num = null)
    {
        bool isOne = num == null ? true : false;
        int count, index;

        if (!isOne)
            count = items.Count;
        else
            count = items.Count > num.Count ? num.Count : items.Count;

        StringBuilder sb = new StringBuilder();
        for (index = 0; index < count; index++)
        {
            var tempData = ItemParentData.GetItem(items[index]);
            if (tempData != null)
            {
                sb.Append(tempData.Name);
                sb.Append("*");
                if (isOne)
                    sb.Append(1);
                else
                    sb.Append(num[index]);
            }
        }

        if (count < items.Count)
        {
            for (; index < items.Count; index++)
            {
                var tempData = ItemParentData.GetItem(items[index]);
                if (tempData != null)
                {
                    sb.Append(tempData.Name);
                    sb.Append("*");
                    sb.Append(1);
                }
            }
        }

        if (sb.Length != 0)
            MogoMsgBox.Instance.ShowMsgBox(string.Format(LanguageData.GetContent(48409), sb)); // "你获得了：" + sb.ToString() + "。"
    }

    public List<int> SortDrops(List<int> data)
    {
        data.Sort(delegate(int id1, int id2)
        {
            ItemParentData a = ItemParentData.GetItem(id1);
            ItemParentData b = ItemParentData.GetItem(id2);

            if (a.quality < b.quality) return 1;
            else if (a.quality > b.quality) return -1;
            else
            {
                if (a.level > b.level) return 1;
                else if (a.level < b.level) return -1;
                else
                {
                    if (a.type > b.type) return -1;
                    else return 1;
                }
            }
        });

        return data;
    }

    public void ShowFightPowerChange(uint num, bool isAdd)
    {
        string msg = string.Empty;
        if (isAdd)
        {
            msg = LanguageData.GetContent(399998, num);
        }
        else
        {
            msg = LanguageData.GetContent(399999, num);
        }
        MogoMsgBox.Instance.ShowFloatTxtForPower(msg);
    }

    public void ShowGetSomething(int itemId, uint num)
    {
        ItemParentData item = ItemParentData.GetItem(itemId);
        if (item == null) return;
        MogoMsgBox.Instance.ShowFloatingTextQueue(LanguageData.GetContent(898, item.Name, num));

    }

    public string FormatDropName(ItemParentData itemParentData)
    {
        if (itemParentData == null)
            return String.Empty;

        string result = String.Empty;

        switch (itemParentData.itemType)
        {
            case 1:
                if (!(itemParentData is ItemEquipmentData))
                    return String.Empty;

                result = LanguageData.dataMap[200005].Format(("[" + QualityColorData.dataMap[itemParentData.quality].color + "]" + (itemParentData as ItemEquipmentData).levelNeed.ToString()).ToString());

                switch (itemParentData.quality)
                {
                    case 1:
                        result += LanguageData.GetContent(47200); // "白色"
                        break;

                    case 2:
                        result += LanguageData.GetContent(47201); // "绿色"
                        break;

                    case 3:
                        result += LanguageData.GetContent(47202); // "蓝色"
                        break;

                    case 4:
                        result += LanguageData.GetContent(47203); // "紫色"
                        break;

                    case 5:
                        result += LanguageData.GetContent(47204); // "橙色"
                        break;

                    case 6:
                        result += LanguageData.GetContent(47205); // "暗金"
                        break;
                }

                switch (itemParentData.type)
                {
                    case 10:
                        result += LanguageData.GetContent(47206); // "武器"
                        break;

                    case 1:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        result += LanguageData.GetContent(47207); // "防具"
                        break;

                    case 2:
                    case 9:
                        result += LanguageData.GetContent(47208); // "饰品"
                        break;
                }

                result += "[-]";
                return result;

            case 2:
                if (!(itemParentData is ItemJewelData))
                    return String.Empty;

                result = LanguageData.dataMap[200005].Format(("[" + QualityColorData.dataMap[itemParentData.quality].color + "]" + (itemParentData as ItemJewelData).level.ToString()).ToString());

                switch (itemParentData.quality)
                {
                    case 1:
                        result += LanguageData.GetContent(47200); // "白色"
                        break;

                    case 2:
                        result += LanguageData.GetContent(47201); // "绿色"
                        break;

                    case 3:
                        result += LanguageData.GetContent(47202); // "蓝色"
                        break;

                    case 4:
                        result += LanguageData.GetContent(47203); // "紫色"
                        break;

                    case 5:
                        result += LanguageData.GetContent(47204); // "橙色"
                        break;
                }

                result += LanguageData.GetContent(47212); // "宝石"
                result += "[-]";
                return result;

            case 3:
                if (!(itemParentData is ItemMaterialData))
                    return String.Empty;

                result = LanguageData.dataMap[200005].Format(("[" + QualityColorData.dataMap[itemParentData.quality].color + "]" + (itemParentData as ItemMaterialData).level.ToString()).ToString());

                switch (itemParentData.quality)
                {
                    case 1:
                        result += LanguageData.GetContent(47200); // "白色"
                        break;

                    case 2:
                        result += LanguageData.GetContent(47201); // "绿色"
                        break;

                    case 3:
                        result += LanguageData.GetContent(47202); // "蓝色"
                        break;

                    case 4:
                        result += LanguageData.GetContent(47203); // "紫色"
                        break;

                    case 5:
                        result += LanguageData.GetContent(47204); // "橙色"
                        break;

                    case 6:
                        result += LanguageData.GetContent(47205); // "暗金"
                        break;
                }

                result += LanguageData.GetContent(47211); // "材料"
                result += "[-]";
                return result;

            case 4:
                if (!(itemParentData is ItemMaterialData))
                    return String.Empty;

                result = LanguageData.dataMap[200005].Format(("[" + QualityColorData.dataMap[itemParentData.quality].color + "]" + (itemParentData as ItemMaterialData).level.ToString()).ToString());

                switch (itemParentData.quality)
                {
                    case 1:
                        result += LanguageData.GetContent(47200); // "白色"
                        break;

                    case 2:
                        result += LanguageData.GetContent(47201); // "绿色"
                        break;

                    case 3:
                        result += LanguageData.GetContent(47202); // "蓝色"
                        break;

                    case 4:
                        result += LanguageData.GetContent(47203); // "紫色"
                        break;

                    case 5:
                        result += LanguageData.GetContent(47204); // "橙色"
                        break;

                    case 6:
                        result += LanguageData.GetContent(47205); // "暗金"
                        break;
                }

                result += LanguageData.GetContent(47209); // "材料"
                result += "[-]";
                return result;
        }

        return result;
    }

    private void OnFumo(int slot)
    {
        FumoManager.Instance.Fumo(slot);
    }

    #region 背包相关查询函数

    /// <summary>
    /// 
    /// </summary>
    /// <returns>null代表无，否则为装备item</returns>
    public ItemEquipment GetRecommendEquipBySlot(int slot)
    {
        //得到该部位的所有可穿装备
        List<ItemEquipment> list = EquipmentInBagList;
        List<ItemEquipment> equipCanUse = new List<ItemEquipment>();
        foreach (ItemEquipment equip in list)
        {
            ItemEquipment temp = null;
            if (slot == 10 || slot == 9)
            {
                if (equip.type == 9)
                    temp = equip;
            }
            else if (slot == 11 && equip.type == 10)
            {
                temp = equip;
            }
            else if (equip.type == slot)
            {
                temp = equip;
            }

            if (temp == null) continue;
            if (temp.levelNeed > MogoWorld.thePlayer.level)
            {
                continue;
            }
            if (temp.vocation != (int)MogoWorld.thePlayer.vocation) continue;
            equipCanUse.Add(temp);
        }
        if (equipCanUse.Count <= 0) return null;
        //找出所有装备中分数最高的
        equipCanUse.Sort(delegate(ItemEquipment a, ItemEquipment b)
        {
            if (a.score > b.score) return -1;
            else return 1;
        });
        return equipCanUse[0];
    }

    static public void SetIcon(int id, UISprite sprite, int num = 0, UILabel label = null, UISprite qulitySprite = null, int posZ = -5)
    {
        //Debug.LogError("SetIcon");
        ItemParentData item = ItemParentData.GetItem(id);
        if (item != null)
        {
            sprite.atlas = MogoUIManager.Instance.GetAtlasByIconName(item.Icon);
            sprite.spriteName = item.Icon;
            MogoUtils.SetImageColor(sprite, item.color, posZ);
            if (label != null)
            {
                if (num < 2)
                    label.text = String.Empty;
                else
                    label.text = num.ToString();
            }
            if (qulitySprite != null)
            {
                if (item.itemType == 2)
                    qulitySprite.spriteName = IconData.blankBox;

                    //SetSpriteColorByQulity(qulitySprite,0);
                else
                {
                    //SetSpriteColorByQulity(qulitySprite,item.quality);
                    qulitySprite.spriteName = IconData.GetIconByQuality(item.quality);
                }
            }
        }
        else
        {
            sprite.spriteName = IconData.none;
            MogoUtils.SetImageColor(sprite, 0, posZ);
            if (qulitySprite != null)
            {
                //SetSpriteColorByQulity(qulitySprite, 0);
                qulitySprite.spriteName = IconData.blankBox;
            }
        }
    }

    static public void SetSpriteColorByQulity(UISprite sp, int qulity)
    {
        switch (qulity)
        {
            case 1: sp.ShowAsButtonWhite(); break;
            case 2: sp.ShowAsButtonGreen(); break;
            case 3: sp.ShowAsButtonBlue(); break;
            case 4: sp.ShowAsButtonPurpose(); break;
            case 5: sp.ShowAsButtonDarkGold(); break;
            case 6: sp.ShowAsButtonDarkGold(); break;
            default: sp.ShowAsButtonNormal(); break;
        }
    }

    static public void TrySetIcon(int id, UISprite sprite, int num = 0, UILabel label = null, UISprite qulitySprite = null)
    {
        //Debug.LogError("TrySetIcon");
        ItemParentData item = ItemParentData.GetItem(id);
        if (item != null)
        {
            MogoUIManager.Instance.TryingSetSpriteName(item.Icon, sprite);
            MogoUtils.SetImageColor(sprite, item.color);
            if (qulitySprite != null)
            {
                if (item.itemType == 2)
                    qulitySprite.spriteName = IconData.blankBox;
                //SetSpriteColorByQulity(qulitySprite, 0);
                else
                    qulitySprite.spriteName = IconData.GetIconByQuality(item.quality);
                //SetSpriteColorByQulity(qulitySprite, item.quality);
            }
        }
        else
        {
            sprite.spriteName = "????";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns>-1证明找不到物品</returns>
    public int GetIndexByItemID(int id)
    {
        foreach (Dictionary<int, ItemParent> dic in m_itemsInBag.Values)
        {
            foreach (ItemParent item in dic.Values)
            {
                if (item.templateId == id) return item.gridIndex;
            }
        }
        return -1;
    }

    public ItemParent GetItemByItemID(int id, bool includeEquipOn = false)
    {
        foreach (Dictionary<int, ItemParent> dic in m_itemsInBag.Values)
        {
            foreach (ItemParent item in dic.Values)
            {
                if (item.templateId == id) return item;
            }
        }
        if (includeEquipOn)
        {
            foreach (ItemParent item in m_itemsOnEquip.Values)
            {
                if (item.templateId == id) return item;
            }
        }
        return null;
    }

    public int GetItemNumById(int id, bool includeEquipOn = false, bool needUnActive = false)
    {
        ItemParentData item = ItemParentData.GetItem(id);
        return GetItemNumByIdAndType(item.id, item.itemType, includeEquipOn, needUnActive);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="type">1装备，2宝石，3普通道具，4材料</param>
    public int GetItemNumByIdAndType(int id, int type, bool includeEquipOn = false, bool needUnActive = false)
    {
        int bagType = type;
        if (bagType == 3) bagType = 1;
        if (bagType == 4) bagType = 3;
        int count = 0;
        foreach (ItemParent item in m_itemsInBag[bagType].Values)
        {
            if (item.templateId != id) continue;
            if (needUnActive && ((ItemEquipment)item).isActive) continue;

            count += item.stack;
        }
        if (includeEquipOn)
        {
            foreach (ItemParent item in m_itemsOnEquip.Values)
            {
                if (item.templateId != id) continue;
                if (needUnActive && ((ItemEquipment)item).isActive) continue;
                count += item.stack;
            }
        }
        return count;
    }

    public int GetCurrentWeaponType()
    {
        return (int)(m_itemsOnEquip[(int)EquipSlot.Weapon].subtype);
    }
    public int GetCurrentWeaponId()
    {
        return (int)(m_itemsOnEquip[(int)EquipSlot.Weapon].templateId);
    }

    /// <summary>
    /// 判断是否比已穿装备好
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool IsEquipmentBetter(int itemId, bool checkLevel = true)
    {
        ItemParentData item = ItemParentData.GetItem(itemId);
        if (item.itemType != 1) return false;

        ItemEquipmentData equip = item as ItemEquipmentData;
        if (checkLevel && equip.levelNeed > MogoWorld.thePlayer.level) return false;

        bool hasSlot = false;//有空位
        bool better = false;//分数更高


        //戒指特殊处理
        if (equip.type == (int)EquipType.Ring)
        {
            hasSlot = (!m_itemsOnEquip.ContainsKey((int)EquipSlot.LeftRing) || !m_itemsOnEquip.ContainsKey((int)EquipSlot.RightRing));
            if (!hasSlot)
            {
                better = equip.GetScore(MogoWorld.thePlayer.level) > m_itemsOnEquip[(int)EquipSlot.LeftRing].GetScore(MogoWorld.thePlayer.level);
                if (!better) better = equip.GetScore(MogoWorld.thePlayer.level) > m_itemsOnEquip[(int)EquipSlot.RightRing].GetScore(MogoWorld.thePlayer.level);
            }
        }
        else
        {
            int slot = item.type == 10 ? 11 : item.type;
            hasSlot = !m_itemsOnEquip.ContainsKey(slot);
            if (!hasSlot)
            {
                better = equip.GetScore(MogoWorld.thePlayer.level) > m_itemsOnEquip[slot].GetScore(MogoWorld.thePlayer.level);
            }
        }
        return hasSlot || better;

    }

    public bool IsRubbish(int itemId, int excludeQulity)
    {
        ItemParentData item = ItemParentData.GetItem(itemId);
        if (item.quality >= excludeQulity) return false;
        return IsRubbish(itemId);
    }

    public bool IsRubbish(int itemId)
    {
        ItemParentData item = ItemParentData.GetItem(itemId);
        if (item.itemType != 1) return false;
        if (item.type == (int)EquipType.Weapon)
        {
            bool hasSlot = false;//有空位
            bool better = false;//分数更高
            int slot = item.type == 10 ? 11 : item.type;
            hasSlot = !m_itemsOnEquip.ContainsKey(slot);
            if (!hasSlot)
            {
                ItemEquipmentData equip = item as ItemEquipmentData;
                better = equip.GetScore(MogoWorld.thePlayer.level) >= m_itemsOnEquip[slot].GetScore(MogoWorld.thePlayer.level);
            }
            return !hasSlot && !better;
        }
        else
        {
            return !IsEquipmentBetter(itemId, false);
        }
    }

    #endregion

    #region 道具tip

    /// <summary>
    /// 背包物品被点击后
    /// </summary>
    /// <param name="index"></param>
    private void OnSelectItemInbag(int index)
    {
        if (!m_itemsInBag[CurrentTagIndex].ContainsKey(index)) return;
        ItemParent item = m_itemsInBag[CurrentTagIndex][index];
        m_selectedItem = item;

        switch (CurrentTagIndex)
        {
            case ITEM_TYPE_EQUIPMENT:
                LoggerHelper.Debug("ShowEquipTip");
                if (m_selectedItem.itemType == 1)
                {
                    ShowEquipTipInBag(m_selectedItem);
                }
                else
                {
                    ShowPropTip(m_selectedItem);
                }
                break;
            case ITEM_TYPE_JEWEL:
                List<ButtonInfo> btnList = new List<ButtonInfo>();
                ButtonInfo btn1 = new ButtonInfo() { action = InsetJewel, text = LanguageData.GetContent(905), id = 905 };
                ButtonInfo btn2 = new ButtonInfo() { action = OnCompose, text = LanguageData.GetContent(906), id = 906 };
                btnList.Add(btn1);
                btnList.Add(btn2);
                ShowJewelTip((ItemJewel)m_selectedItem, btnList);
                break;
            case ITEM_TYPE_MATERIAL:
                ShowMaterialTip(m_selectedItem);
                break;
        }

    }

    public void ShowCurrentEquipInfo(ItemParent item, int _level)
    {
        ItemEquipment equip = (ItemEquipment)item;
        EquipTipViewData data = GetEquipInfoByItem(equip, _level);
        //MenuUIViewManager view = MenuUIViewManager.Instance;
        EquipTipManager view = EquipTipManager.Instance;

        //界面静态部分
        view.SetEquipDetailInfoImageCurrent(data.iconName);
        view.ShowEquipDetailInfoImageUsedCurrent(data.isEquipOn);
        view.SetEquipDetailInfoNameCurrent(data.name);
        view.SetEquipDetailInfoImageBgCurrent(IconData.GetIconByQuality(equip.quality));
        view.SetEquipDetailInfoScoreNumCurrent(equip.score);

        //界面动态排版部分
        List<string> attrList = equip.GetAttrDescriptionList(_level);
        List<string> jewelList = new List<string>();
        foreach (string s in data.jewelHoles)
        {
            if (s == "") continue;
            jewelList.Add(s);
        }

        string suitName = "";
        List<string> suitAttr = new List<string>();
        if (equip.suitId > 0 && equip.isActive)
        {
            suitName = ItemSuitEquipmentsData.GetSuitName(equip.suitId);
            int suitPutOnNum = GetSuitPutonNum(equip.suitId);
            int suitMaxNum = ItemSuitEquipmentsData.GetSuitMaxNum(equip.suitId);
            suitName = String.Concat(suitName, "(", suitPutOnNum, "/", suitMaxNum, ")");

            suitAttr = ItemSuitEquipmentsData.GetSuitAttrList(equip.suitId, suitPutOnNum);
        }

        string levelDesp = equip.levelDesp;
        view.SetEquipDetailInfoLevelTextCurrent(levelDesp);


        string vocation = LanguageData.dataMap[equip.vocation].content;
        if (equip.vocation != (int)MogoWorld.thePlayer.vocation)
        {
            vocation = string.Concat("[ff0000]", vocation, "[-]");
        }
        view.ShowEquipTipCurrent(suitName, suitAttr, attrList, jewelList, data.jewelSlotIcons, vocation, null);

    }

    private void ShowMaterialTip(ItemParent item)
    {
        //Debug.LogError("ShowMaterialTip:" + item.id);
        ItemMaterial material = (ItemMaterial)item;

        List<ButtonInfo> btnList = new List<ButtonInfo>();
        ButtonInfo btn;
        if (material.type == 1)
        {
            btn = new ButtonInfo() { action = OnMaterialTipEnhance, text = LanguageData.GetContent(907), id = 907 };
            btnList.Add(btn);
        }
        if (CanMaterialExchangeEquip(item))
        {
            btn = new ButtonInfo() { action = () => { OnExchangeEquip(item); }, id = 899, text = LanguageData.GetContent(899) };
            btnList.Add(btn);
        }
        if (material.price > 0)
        {
            btn = new ButtonInfo() { action = OnMaterialTipSell, text = LanguageData.GetContent(908), id = 908 };
            btnList.Add(btn);
        }

        ShowMaterialTip(material.templateId, btnList, material.stack, material.maxStack);

    }

    public void ShowMaterialTip(int id, List<ButtonInfo> btnList, int stack = 0, int maxStack = 0)
    {
        ItemMaterialData material = ItemMaterialData.dataMap.Get(id);
        EquipTipManager view = EquipTipManager.Instance;

        view.SetEquipDetailInfoName(material.Name);
        Mogo.Util.LoggerHelper.Debug("material.Name:" + material.Name);
        Mogo.Util.LoggerHelper.Debug("material.Name:" + material.id);
        view.SetEquipDetailInfoImage(material.Icon, material.color);
        view.SetEquipDetailInfoImageBg(IconData.GetIconByQuality(material.quality));
        string level = material.level + "";
        string desp = LanguageData.GetContent(47210) + material.Description;
        string stackStr = "";
        string price = "";
        if (maxStack > 0)
        {
            stackStr = stack + "/" + maxStack + "";
        }

        if (material.price > 0)
        {
            price = material.price + "";
        }


        view.ShowMaterialTip(level, desp, stackStr, price, btnList);
    }

    private void ShowPropTip(ItemParent item)
    {
        ItemMaterial material = (ItemMaterial)item;

        string stack = "";
        if (material.maxStack > 1)
        {
            stack = material.stack + "/" + material.maxStack + "";
            //view.SetPropTipStack(material.stack + "/" + material.maxStack + "");
        }

        List<ButtonInfo> btnList = new List<ButtonInfo>();
        ButtonInfo btn;
        if (CanMaterialExchangeEquip(item))
        {
            btn = new ButtonInfo() { action = () => { OnExchangeEquip(item); }, id = 899, text = LanguageData.GetContent(899) };
            btnList.Add(btn);
        }

        //激活套装材料
        if (item.type == 8)
        {
            btn = new ButtonInfo() { action = () => { OnActivate(item); }, id = 899, text = LanguageData.GetContent(899) };
            btnList.Add(btn);
        }

        if (material.effectId > 0 && material.type != 8)
        {
            btn = new ButtonInfo() { action = OnPropUse, text = LanguageData.GetContent(909), id = 909 };
            btnList.Add(btn);
        }
        if (material.price > 0)
        {
            btn = new ButtonInfo() { action = OnMaterialTipSell, text = LanguageData.GetContent(908), id = 908 };
            btnList.Add(btn);
        }

        ShowPropTip(item.templateId, btnList, stack);
    }

    private void OnActivate(ItemParent item)
    {
        List<EquipExchangeViewData> viewDataList = GetActivateViewDataList(ItemParentData.GetItem(item.templateId));
        EquipExchangeUIViewManager.Instance.Show(0, viewDataList, false);
        EquipTipManager.Instance.CloseEquipTip();
        EquipExchangeUIViewManager.Instance.SetGold(MogoWorld.thePlayer.gold + "");
        EquipExchangeUIViewManager.Instance.SetMaterial(item.templateId, GetItemNumById(item.templateId) + "");
    }

    private void RefreshActivateUI(ItemParentData item)
    {
        List<EquipExchangeViewData> viewDataList = GetActivateViewDataList(item);
        EquipExchangeUIViewManager.Instance.RefreshUI(viewDataList);
        EquipTipManager.Instance.CloseEquipTip();
        EquipExchangeUIViewManager.Instance.SetGold(MogoWorld.thePlayer.gold + "");
        EquipExchangeUIViewManager.Instance.SetMaterial(item.id, GetItemNumById(item.id) + "");
    }

    private List<EquipExchangeViewData> GetActivateViewDataList(ItemParentData item)
    {
        List<EquipExchangeViewData> viewDataList = new List<EquipExchangeViewData>();
        ItemSuitEquipmentsData suitData = ItemSuitEquipmentsData.dataMap.Get(item.effectId);

        List<int> equipIdList = suitData.GetEquipList((int)MogoWorld.thePlayer.vocation);

        //if (equipIdList == null) Debug.LogError("fuck!");

        for (int i = 0; i < equipIdList.Count; i++)
        {
            EquipExchangeViewData data = new EquipExchangeViewData();
            data.itemId = equipIdList[i];
            data.goldNum = "x1";
            data.materialId = item.id;
            data.materialNum = "x1";
            if (GetItemNumById(item.id) <= 0)
            {
                data.materialNum = MogoUtils.GetRedString(data.materialNum);
            }
            if (GetItemNumById(equipIdList[i], true, true) <= 0)
            {
                data.goldNum = MogoUtils.GetRedString(data.goldNum);
            }

            data.title = ItemParentData.GetItem(equipIdList[i]).Name;
            var index = i;
            data.goldIconId = equipIdList[i];
            data.onExchange = () =>
            {
                MogoWorld.thePlayer.RpcCall("ActivedSuitEquipmentReq", equipIdList[index]);
            };

            viewDataList.Add(data);
        }
        return viewDataList;
    }

    public void ShowPropTip(int id, List<ButtonInfo> btnList, string stack = "")
    {
        ItemMaterialData material = ItemMaterialData.dataMap.Get(id);
        EquipTipManager view = EquipTipManager.Instance;

        string desp = LanguageData.GetContent(47210) + material.Description;
        string price = "";
        if (material.price > 0)
        {
            price = material.price + "";
        }
        view.SetEquipDetailInfoName(material.Name);
        view.SetEquipDetailInfoImage(material.Icon, material.color);
        view.SetEquipDetailInfoImageBg(IconData.GetIconByQuality(material.quality));

        view.ShowPropTip(desp, stack, price, btnList);
    }

    public void ShowJewelTip(ItemJewel jewel, List<ButtonInfo> btnList)
    {

        string stack = jewel.stack + "/" + jewel.maxStack + "";
        ShowJewelTip(jewel.templateId, btnList, stack);
    }

    public void ShowJewelTip(int jewelId, List<ButtonInfo> btnList, string stack = "")
    {
        EquipTipManager view = EquipTipManager.Instance;
        ItemJewelData jewel = ItemJewelData.dataMap[jewelId];

        string level = jewel.level + "";
        string type = jewel.typeName;
        string desp = LanguageData.GetContent(47210) + jewel.effectDescriptionStr;
        view.SetEquipDetailInfoName(jewel.Name);
        view.SetEquipDetailInfoImage(jewel.Icon, jewel.color);
        view.SetEquipDetailInfoImageBg(IconData.blankBox);

        view.ShowJewelTip(level, type, desp, stack, btnList);
    }

    private void ShowEquipTipInBag(ItemParent item)
    {
        //显示选中装备tip
        List<ButtonInfo> btnList = new List<ButtonInfo>();
        ButtonInfo btn = new ButtonInfo() { action = OnEquip, text = LanguageData.GetContent(900), id = 900 };
        //显示当前装备tip
        int slot = (item.type == 10 ? 11 : item.type);

        //戒指的情况
        if (item.type == (int)EquipType.Ring)
        {
            if (m_itemsOnEquip.ContainsKey(item.type) && m_itemsOnEquip.ContainsKey(item.type + 1))
            {
                btn.text = LanguageData.GetContent(901);
                btn.id = 901;
                ItemEquipment item1 = m_itemsOnEquip.Get(item.type);
                ItemEquipment item2 = m_itemsOnEquip.Get(item.type + 1);
                ItemEquipment item3 = item1.score < item2.score ? item1 : item2;
                ShowCurrentEquipInfo(item3, MogoWorld.thePlayer.level);
            }
        }
        else//非戒指的情况
        {
            if (m_itemsOnEquip.ContainsKey(slot))
            {
                LoggerHelper.Debug("has equip on:" + m_itemsOnEquip[slot].templateId);
                btn.text = LanguageData.GetContent(901);
                btn.id = 901;
                ShowCurrentEquipInfo(m_itemsOnEquip[slot], MogoWorld.thePlayer.level);
            }
        }

        btnList.Add(btn);

        //Debug.LogError(item.effectId);
        //装备升级
        if (EquipUpgradeManager.Instance.CanUpgrade(item))
        {
            //Debug.LogError(item.effectId);
            btn = new ButtonInfo() { action = OnUpGragdeEquip, text = LanguageData.GetContent(902), id = 902 };
            btnList.Add(btn);
        }


        btn = new ButtonInfo() { action = ShowItem, text = LanguageData.GetContent(903), id = 903 };
        btnList.Add(btn);
        //btn = new ButtonInfo() { action = OnDecompose, text = LanguageData.GetContent(904), id = 904 };
        //btnList.Add(btn);


        ShowEquipTip((ItemEquipment)item, btnList, MogoWorld.thePlayer.level);


        LoggerHelper.Debug("ShowEquipTip done");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="equip"></param>
    /// <param name="buttonList">若无按钮即传null</param>
    public void ShowEquipTip(ItemEquipment equip, List<ButtonInfo> buttonList, int _level, bool needFumoInfo = false, int slot = -1)
    {
        EquipTipViewData data = GetEquipInfoByItem(equip, _level);
        //MenuUIViewManager view = MenuUIViewManager.Instance;
        EquipTipManager view = EquipTipManager.Instance;

        //界面静态部分
        //view.SetEquipDetailInfoImage(data.iconName);
        view.ShowEquipDetailInfoImageUsed(data.isEquipOn);
        view.SetEquipDetailInfoName(data.name);//
        view.SetEquipImage(equip.templateId);
        view.SetEquipDetailInfoScoreNum(equip.score);
        //view.SetEquipDetailInfoImageBg(IconData.GetIconByQuality(equip.quality));

        //界面动态排版部分
        List<string> attrList = equip.GetAttrDescriptionList(_level);
        List<string> jewelList = new List<string>();
        foreach (string s in data.jewelHoles)
        {
            if (s == "") continue;
            jewelList.Add(s);
        }

        string suitName = "";
        List<string> suitAttr = new List<string>();
        if (equip.suitId > 0)
        {
            suitName = ItemSuitEquipmentsData.GetSuitName(equip.suitId);
            if (!equip.isActive) suitName += LanguageData.GetContent(1137);
            int suitPutOnNum = GetSuitPutonNum(equip.suitId);
            int suitMaxNum = ItemSuitEquipmentsData.GetSuitMaxNum(equip.suitId);
            suitName = String.Concat(suitName, "(", suitPutOnNum, "/", suitMaxNum, ")");
            suitAttr = ItemSuitEquipmentsData.GetSuitAttrList(equip.suitId, suitPutOnNum);
        }

        string levelDesp = equip.levelDesp;
        view.SetEquipDetailInfoLevelText(levelDesp);

        string vocation = LanguageData.dataMap[equip.vocation].content;
        vocation = LanguageData.GetContent(912, vocation);
        if (equip.vocation != (int)MogoWorld.thePlayer.vocation)
        {
            vocation = MogoUtils.GetRedString(vocation);

        }
        view.SetVocationNeedText(vocation);
        FumoTipUIInfo fumoInfo = null;
        if (needFumoInfo)
        {
            fumoInfo = FumoManager.Instance.GetFumoTipUIInfo(slot);
        }
        view.ShowEquipTip(suitName, suitAttr, attrList, jewelList, data.jewelSlotIcons, buttonList, fumoInfo);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="equip"></param>
    /// <param name="buttonList">若无按钮即传null</param>
    private void ShowEquipTip(int templateId, List<ButtonInfo> buttonList, int _level, bool isActive = false)
    {
        ItemEquipment ie = GetItemEquipmentByInstance(ItemInstance.GetEmptyInstance(templateId));
        ie.isActive = isActive;
        ShowEquipTip(ie, buttonList, _level);
        //ItemEquipmentData equip = ItemEquipmentData.dataMap[templateId];

        //EquipTipManager view = EquipTipManager.Instance;

        ////界面静态部分
        //view.SetEquipDetailInfoImage(equip.Icon);
        //view.ShowEquipDetailInfoImageUsed(false);
        //view.SetEquipDetailInfoName(equip.Name);
        //view.SetEquipDetailInfoImageBg(IconData.GetIconByQuality(equip.quality));

        ////界面动态排版部分
        //List<string> attrList = equip.GetAttrDescriptionList(equip.levelNeed);
        //List<string> jewelList = new List<string>();

        //for (int i = 0; i < equip.jewelSlot.Count; i++)
        //{
        //    jewelList.Add("未镶嵌");
        //}

        //view.ShowEquipTip(attrList, jewelList, equip.levelNeed + "", LanguageData.dataMap[equip.vocation].content, buttonList);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="equip"></param>
    /// <param name="buttonList">若无按钮即传null</param>
    public void ShowEquipTip(int templateId, List<ButtonInfo> buttonList, List<int> slots, int _level)
    {
        ItemEquipment ie = GetItemEquipmentByInstance(ItemInstance.GetEmptyInstance(templateId));
        ie.jewelSlots = slots;
        ShowEquipTip(ie, buttonList, _level);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id">道具id</param>
    /// <param name="buttonList">按钮列表</param>
    public void ShowItemTip(int id, List<ButtonInfo> buttonList)
    {
        ItemParentData item = ItemParentData.GetItem(id);
        if (item == null) return;
        switch (item.itemType)
        {
            case 1://装备
                ShowEquipTip(id, buttonList, MogoWorld.thePlayer.level);
                break;
            case 2://宝石
                ShowJewelTip(id, buttonList);
                break;
            case 3://道具
                ShowPropTip(id, buttonList);
                break;
            case 4://材料
                ShowMaterialTip(id, buttonList);
                break;
        }
    }


    public void ShowItemTip(int id)
    {
        ShowItemTip(id, false);
    }

    public void ShowItemTip(int id, bool isActive)
    {
        ItemParentData item = ItemParentData.GetItem(id);
        if (item == null) return;
        switch (item.itemType)
        {
            case 1://装备
                ShowEquipTip(id, null, MogoWorld.thePlayer.level, isActive);
                break;
            case 2://宝石
                ShowJewelTip(id, null);
                break;
            case 3://道具
                ShowPropTip(id, null);
                break;
            case 4://材料
                ShowMaterialTip(id, null);
                break;
        }
    }

    /// <summary>
    /// 角色界面中装备被点时
    /// </summary>
    /// <param name="_typeId"></param>
    public void OnEquipGridUp(int _typeId)
    {
        if (!m_itemsOnEquip.ContainsKey(_typeId))
            return;
        LoggerHelper.Debug("OnEquipGridUp:" + _typeId);
        ItemEquipment item = m_itemsOnEquip[_typeId];
        m_selectedItem = item;

        List<ButtonInfo> btnList = new List<ButtonInfo>();
        ButtonInfo btn;

        if (item.enchant != null && item.enchant.Count > 0)
        {
            btn = new ButtonInfo()
            {
                action = () => { OnFumo(_typeId); },
                text = LanguageData.GetContent(1349),
                id = 1349
            };
            btnList.Add(btn);
        }

        btn = new ButtonInfo() { action = ShowItem, text = LanguageData.GetContent(903), id = 903 };
        btnList.Add(btn);

        btn = new ButtonInfo() { action = EquipToInsetJewel, text = LanguageData.GetContent(905), id = 905 };
        btnList.Add(btn);

        btn = new ButtonInfo() { action = StrengthEquip, text = LanguageData.GetContent(907), id = 907 };
        btnList.Add(btn);

        //ButtonInfo btn3 = new ButtonInfo() { action = OnRemoveEquip, text = "卸下" };
        //if (EquipUpgradeManager.Instance.CanUpgrade(item))
        //{
        //    //Debug.LogError(item.effectId);
        //    btn = new ButtonInfo() { action = OnUpGragdeEquip, text = LanguageData.GetContent(902), id = 902 };
        //    btnList.Add(btn);
        //}
        //btnList.Add(btn3);

        ShowEquipTip(item, btnList, MogoWorld.thePlayer.level, true, _typeId);
        //ShowEquipInfo(item);
    }

    #endregion

    /// <summary>
    /// ERR_ITEM_ACTIVE_OK                          = 0,    --兑换成功
    ///ERR_ITEM_ACTIVE_CFG                         = 1,    --配置有问题
    ///ERR_ITEM_ACTIVE_NO                          = 2,    --不存在可激活的装备
    ///ERR_ITEM_ACTIVE_WRONG                       = 3,    --不是套装ID
    ///ERR_ITEM_ACTIVE_UNCOSTS                     = 4,    --激活消耗不足
    /// </summary>
    /// <param name="errorId"></param>
    /// <param name="typeId"></param>
    /// <param name="bagType"></param>
    public void ActivedSuitEquipmentResp(byte errorId, uint typeId, byte bagType)
    {
        //提示 + 刷新界面
        switch (errorId)
        {
            case 0:
                ItemParentData item = ItemParentData.GetItem((int)typeId);

                if (bagType == ITEM_TYPE_EQUIPMENT)
                {
                    MogoMsgBox.Instance.ShowFloatingText(LanguageData.GetContent(1131, item.Name));
                }
                else
                {
                    MogoMsgBox.Instance.ShowFloatingText(LanguageData.GetContent(1130, item.Name));
                }
                ItemSuitEquipmentsData data = ItemSuitEquipmentsData.dataMap.Get((item as ItemEquipmentData).suitId);
                RefreshActivateUI(ItemParentData.GetItem(data.costs));
                break;
            default:
                MogoMsgBox.Instance.ShowFloatingText(LanguageData.GetContent(1132 + errorId));
                break;
        }
    }
}