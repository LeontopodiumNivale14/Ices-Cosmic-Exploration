using System.Collections.Generic;

namespace ICE.Utilities;

public static unsafe class GatheringUtil
{

    public class GatheringActions
    {
        /// <summary>
        /// Internal name for myself to know wtf this is
        /// </summary>
        public string ActionName { get; set; }
        /// <summary>
        /// Sheet name
        /// </summary>
        public string BtnName { get; set; }
        /// <summary>
        /// Botanist Action ID
        /// </summary>
        public uint BtnActionId { get; set; }
        /// <summary>
        /// Sheet name
        /// </summary>
        public string MinName { get; set; }
        /// <summary>
        /// Miner Action ID
        /// </summary>
        public uint MinActionId { get; set; }
        /// <summary>
        /// If it has a status, the ID associated with it
        /// </summary>
        public uint StatusId { get; set; }
        /// <summary>
        /// The status name attached to it (personal use)
        /// </summary>
        public string StatusName { get; set; }
        /// <summary>
        /// The amount of GP required for the skill
        /// </summary>
        public int RequiredGp { get; set; }
    }

    public static Dictionary<string, GatheringActions> GathActionDict = new()
    {
        { "BoonIncrease1", new GatheringActions
        {
            ActionName = "Pioneer's Gift I",
            BtnName = "",
            BtnActionId = 21178,
            MinName = "",
            MinActionId = 21177,
            StatusId = 2666,
            StatusName = "Gift of the Land",
            RequiredGp = 50,
        }},
        { "BoonIncrease2", new GatheringActions
        {
            ActionName = "Pioneer's Gift II",
            BtnName = "",
            BtnActionId = 25590,
            MinName = "",
            MinActionId = 25589,
            StatusId = 759,
            StatusName = "Gift of the Land II",
            RequiredGp = 100,
        }},
        { "Tidings", new GatheringActions
        {
            ActionName = "Nophica's Tidings",
            BtnName = "",
            BtnActionId = 21204,
            MinName = "",
            MinActionId = 21203,
            StatusId = 2667,
            StatusName = "Gatherer's Bounty",
            RequiredGp = 200,
        }},
        { "YieldI", new GatheringActions
        {
            ActionName = "Blessed Harvest",
            BtnName = "",
            BtnActionId = 222,
            MinName = "",
            MinActionId = 239,
            StatusId = 219,
            StatusName = "Gathering Yield Up",
            RequiredGp = 400,
        }},
        { "YieldII", new GatheringActions
        {
            ActionName = "Blessed Harvest II",
            BtnName = "",
            BtnActionId = 224,
            MinName = "",
            MinActionId = 241,
            StatusId = 219,
            StatusName = "Gathering Yield Up",
            RequiredGp = 500,
        }},
        { "IntegrityIncrease", new GatheringActions
        {
            ActionName = "Ageless Words",
            BtnName = "",
            BtnActionId = 215,
            MinName = "",
            MinActionId = 232,
            RequiredGp = 300,
        }},
        { "BonusIntegrityChance", new GatheringActions
        {
            ActionName = "Wise of the World",
            BtnName = "",
            BtnActionId = 26522,
            MinName = "",
            MinActionId = 26521,
            StatusId = 2765,
            StatusName = "",
            RequiredGp = 0,
        }},
        { "BountifulYieldII", new GatheringActions
        {
            ActionName = "Bountiful Yield/Harvest II",
            BtnName = "",
            BtnActionId = 273,
            MinName = "",
            MinActionId = 272,
            StatusId = 1286,
            StatusName = "",
            RequiredGp = 100,
        }},
    };

    /* First things first, there's several types of missions for gathering
     * 1 Quantity Limited(Gather x amount on limited amount of nodes)
     * 2 Quantity(Gather x amount, gather more for increased score)
     * 3 Timed(Gather x amount in the time limit)
     * 4 Chain(Increase score based on chain)
     * 5 Gatherer's Boon (Increase score by hitting boon % chance)
     * 6 Chain + Boon(Get score from chain nodes + boon % chance)
     * 7 Collectables(This is going to be annoying)
     * 8 Time Steller Reduction(???) (Assuming Collectables -> Reducing for score...fuck)
     */

    public static Dictionary<Vector2, uint> Nodeset = new()
    {
        // Miner Set
        { new Vector2(-119, -175), 1 },
        { new Vector2(-168, -181), 2 },
        { new Vector2(96, 259), 3 },
        { new Vector2(65, -431), 4 },
        { new Vector2(-475, 135),  5 },
        { new Vector2(73, -482), 6 },
        { new Vector2(-463, -729), 7 },
        { new Vector2(-690, -752), 8 },
        { new Vector2(-669, -515), 9 },

        // Botanist Set
        { new Vector2(-278, -13), 11 },
        { new Vector2(225, 83), 12 },
        { new Vector2(232, -50), 13 },
        { new Vector2(456, 221), 14 },
        { new Vector2(-121, 368), 15 },
        { new Vector2(455, 243), 16 },
        { new Vector2(506, 682), 17 },
        { new Vector2(609, 478), 18 },
        { new Vector2(527, 630), 19 },
        { new Vector2(-706, 564), 20 },

        // Criticals -- Placeholders
        { new Vector2(-503, -324), 21 },
        { new Vector2(-131, -365), 22 },
        { new Vector2(-270, 140), 23 },
        // { new Vector2(566, -908), 24 },
        { new Vector2(188, -201), 25 },
        { new Vector2(748, 101), 26 },

        // New Area -- Just Placeholders for now
        // { new Vector2(416, -737), 27 },
        // { new Vector2(414, -755), 28 },
        // { new Vector2(538, -83), 29 },
        // { new Vector2(-706, -464), 30}
        // { new Vector2(157, -37), 31}
        // { new Vector2(324, -41), 32} 
    };

    public class GathNodeInfo
    {
        public Vector3 Position { get; set; }
        public Vector3 LandZone { get; set; }
        public uint NodeId { get; set; }
        public int GatheringType { get; set; }
        public int ZoneId { get; set; }
        public uint NodeSet { get; set; }
    }

    public static List<GathNodeInfo> MoonNodeInfoList = new()
    {
        new GathNodeInfo // Template for how it should be kept
        {
            Position = new Vector3(0, 0, 0), // The stored coords of the node, rounded up 2
            LandZone = new Vector3(0, 0, 0), // The position/place where you want to stand to gather
            NodeId = 0, // The dataId of said node
            GatheringType = 0, // What type is it? 2 = Miner, 3 = Btn
            ZoneId = 0, // Matters moreso for future moon... moons? Just safety profing
            NodeSet = 0 // Which set of gathering points does this belong to? Ties together all the nodes into one for documentation
                        // Going to see if I can tie it by missionId set maybe... things to dig into
        },

        #region Miner Nodes

            #region (Set #1) (8 Chain Node)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35040,
                Position = new Vector3 (-50.68f, 19.41f, -208.97f),
                LandZone = new Vector3 (-50.64f, 18.41f, -209.93f),
                GatheringType = 2,
                NodeSet = 1
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35039,
                Position = new Vector3 (-73.1f, 20.16f, -204.29f),
                LandZone = new Vector3 (-73.45f, 19.12f, -205.25f),
                GatheringType = 2,
                NodeSet = 1
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35038,
                Position = new Vector3 (-90.95f, 22.52f, -194.11f),
                LandZone = new Vector3 (-91.03f, 21.15f, -194.67f),
                GatheringType = 2,
                NodeSet = 1
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35037,
                Position = new Vector3 (-109.77f, 24.82f, -187.37f),
                LandZone = new Vector3 (-109.83f, 23.74f, -188.08f),
                GatheringType = 2,
                NodeSet = 1
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35036,
                Position = new Vector3 (-129.53f, 27.94f, -170.34f),
                LandZone = new Vector3 (-129.85f, 26.59f, -170.81f),
                GatheringType = 2,
                NodeSet = 1
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35035,
                Position = new Vector3 (-135.65f, 28.2f, -156.82f),
                LandZone = new Vector3 (-135.94f, 27f, -157.07f),
                GatheringType = 2,
                NodeSet = 1
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35034,
                Position = new Vector3 (-142.32f, 27.14f, -139.7f),
                LandZone = new Vector3 (-142.57f, 25.67f, -140.04f),
                GatheringType = 2,
                NodeSet = 1
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35033,
                Position = new Vector3 (-153.94f, 24.31f, -124.47f),
                LandZone = new Vector3 (-154.36f, 23.29f, -124.61f),
                GatheringType = 2,
                NodeSet = 1
            },

            #endregion

            #region (Set #2) (Close to 8 chain)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35062,
                Position = new Vector3 (-110.86f, 20.37f, -226.34f),
                LandZone = new Vector3 (-110.73f, 19.3f, -225.31f),
                GatheringType = 2,
                NodeSet = 2
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35061,
                Position = new Vector3 (-117.08f, 20.9f, -230.27f),
                LandZone = new Vector3 (-118.01f, 20f, -230.48f),
                GatheringType = 2,
                NodeSet = 2
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35057,
                Position = new Vector3 (-226.12f, 29.29f, -176.71f),
                LandZone = new Vector3 (-225.34f, 28.75f, -176.68f),
                GatheringType = 2,
                NodeSet = 2
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35058,
                Position = new Vector3 (-228.37f, 27.66f, -167.48f),
                LandZone = new Vector3 (-227.69f, 27.39f, -167.57f),
                GatheringType = 2,
                NodeSet = 2
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35059,
                Position = new Vector3 (-170.4f, 20.94f, -116.41f),
                LandZone = new Vector3 (-170.99f, 20.28f, -116.77f),
                GatheringType = 2,
                NodeSet = 2
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35060,
                Position = new Vector3 (-162.69f, 22.16f, -124.16f),
                LandZone = new Vector3 (-162.84f, 21.93f, -125.09f),
                GatheringType = 2,
                NodeSet = 2
            },

            #endregion

            #region (Set #3) (FARRR Away)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35066,
                Position = new Vector3 (60.94f, 22.63f, 204.94f),
                LandZone = new Vector3 (60.95f, 21.53f, 205.38f),
                GatheringType = 2,
                NodeSet = 3
            },

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35065,
                Position = new Vector3 (40.95f, 21.06f, 208.7f),
                LandZone = new Vector3 (41.39f, 19.97f, 209.17f),
                GatheringType = 2,
                NodeSet = 3
            },

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35063,
                Position = new Vector3 (65.52f, 18.94f, 318.76f),
                LandZone = new Vector3 (65.6f, 18.21f, 318.18f),
                GatheringType = 2,
                NodeSet = 3
            },

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35064,
                Position = new Vector3 (75.12f, 19.39f, 322.87f),
                LandZone = new Vector3 (75.45f, 18.57f, 322.06f),
                GatheringType = 2,
                NodeSet = 3
            },

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35068,
                Position = new Vector3 (172.32f, 23.64f, 275.08f),
                LandZone = new Vector3 (172.16f, 22.95f, 274.95f),
                GatheringType = 2,
                NodeSet = 3
            },

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35067,
                Position = new Vector3 (174.37f, 23.41f, 267.84f),
                LandZone = new Vector3 (174.24f, 22.82f, 267.61f),
                GatheringType = 2,
                NodeSet = 3
            },


        #endregion

            #region (Set #4)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35041,
                Position = new Vector3 (70.23f, 35.63f, -370.83f),
                LandZone = new Vector3 (69.72f, 35.02f, -370.42f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35042,
                Position = new Vector3 (56.98f, 36.75f, -385.37f),
                LandZone = new Vector3 (57.49f, 35.95f, -384.87f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35043,
                Position = new Vector3 (78.47f, 39.5f, -424.33f),
                LandZone = new Vector3 (77.97f, 39.06f, -424.98f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35044,
                Position = new Vector3 (56.85f, 40.02f, -444.27f),
                LandZone = new Vector3 (57.26f, 39.27f, -444f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35045,
                Position = new Vector3 (56.59f, 40.24f, -453.96f),
                LandZone = new Vector3 (57.03f, 39.6f, -454.24f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35046,
                Position = new Vector3 (45.23f, 41.43f, -473.65f),
                LandZone = new Vector3 (45.55f, 40.41f, -473.4f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35047,
                Position = new Vector3 (49.7f, 41.83f, -481.26f),
                LandZone = new Vector3 (50.06f, 40.96f, -481.57f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35048,
                Position = new Vector3 (60.49f, 43.1f, -499.58f),
                LandZone = new Vector3 (60.52f, 42.33f, -499.64f),
                GatheringType = 2,
                NodeSet = 4
            },

            #endregion

            #region (Set #5)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35072,
                Position = new Vector3 (-430.47f, 42.53f, 96.48f),
                LandZone = new Vector3 (-430.96f, 42.04f, 96.05f),
                GatheringType = 2,
                NodeSet = 5
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35071,
                Position = new Vector3 (-438.91f, 43.87f, 101.09f),
                LandZone = new Vector3 (-438.25f, 42.71f, 101.14f),
                GatheringType = 2,
                NodeSet = 5
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35074,
                Position = new Vector3 (-543.83f, 41.39f, 78.79f),
                LandZone = new Vector3 (-543.35f, 40.24f, 78.7f),
                GatheringType = 2,
                NodeSet = 5
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35073,
                Position = new Vector3 (-542.48f, 44.36f, 91.48f),
                LandZone = new Vector3 (-542.08f, 43.15f, 91.97f),
                GatheringType = 2,
                NodeSet = 5
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35070,
                Position = new Vector3 (-400.24f, 45f, 191.3f),
                LandZone = new Vector3 (-400.94f, 44.26f, 190.28f),
                GatheringType = 2,
                NodeSet = 5
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35069,
                Position = new Vector3 (-394.25f, 44.13f, 189.71f),
                LandZone = new Vector3 (-394.35f, 43.28f, 188.94f),
                GatheringType = 2,
                NodeSet = 5
            },

            #endregion

            #region (Set #6)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35078,
                Position = new Vector3 (77.56f, 39.48f, -424f),
                LandZone = new Vector3 (76.97f, 38.91f, -424.55f),
                GatheringType = 2,
                NodeSet = 6
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35077,
                Position = new Vector3 (87.1f, 39.77f, -424.88f),
                LandZone = new Vector3 (87.04f, 39.23f, -425.47f),
                GatheringType = 2,
                NodeSet = 6
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35076,
                Position = new Vector3 (142.59f, 47.46f, -491.28f),
                LandZone = new Vector3 (142.78f, 46.57f, -490.84f),
                GatheringType = 2,
                NodeSet = 6
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35075,
                Position = new Vector3 (135.91f, 47.36f, -500.54f),
                LandZone = new Vector3 (135.63f, 46.7f, -500.5f),
                GatheringType = 2,
                NodeSet = 6
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35079,
                Position = new Vector3 (32.70f, 43.50f, -521.08f),
                LandZone = new Vector3 (32.70f, 43.50f, -521.08f),
                GatheringType = 2,
                NodeSet = 6
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35080,
                Position = new Vector3 (15.94f, 44.39f, -526.71f),
                LandZone = new Vector3 (15.66f, 43.36f, -526.49f),
                GatheringType = 2,
                NodeSet = 6
            },

            #endregion

            #region (Set #7)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35056,
                Position = new Vector3 (-419.94f, 66.8f, -692.3f),
                LandZone = new Vector3 (-420.07f, 66.15f, -691.43f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35055,
                Position = new Vector3 (-428.39f, 67.89f, -704.01f),
                LandZone = new Vector3 (-428.84f, 67.15f, -703.4f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35054,
                Position = new Vector3 (-447.28f, 68.51f, -707.15f),
                LandZone = new Vector3 (-446.41f, 67.61f, -707.17f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35053,
                Position = new Vector3 (-461.66f, 69.71f, -713.83f),
                LandZone = new Vector3 (-462.18f, 68.99f, -713.69f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35052,
                Position = new Vector3 (-462.91f, 71.27f, -731.6f),
                LandZone = new Vector3 (-463.43f, 70.55f, -731.26f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35051,
                Position = new Vector3 (-467.54f, 73.48f, -747.74f),
                LandZone = new Vector3 (-468f, 72.64f, -747.89f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35050,
                Position = new Vector3 (-469.04f, 76.77f, -770.29f),
                LandZone = new Vector3 (-469.29f, 76.02f, -769.88f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35049,
                Position = new Vector3 (-492.64f, 78.8f, -777f),
                LandZone = new Vector3 (-492.64f, 78.01f, -776.5f),
                GatheringType = 2,
                NodeSet = 7
            },

            #endregion

            #region (Set #8)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35086,
                Position = new Vector3 (-635.22f, 73.97f, -704.67f),
                LandZone = new Vector3 (-636.22f, 73.17f, -703.98f),
                GatheringType = 2,
                NodeSet = 8
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35085,
                Position = new Vector3 (-621.59f, 75.08f, -715.89f),
                LandZone = new Vector3 (-620.05f, 74.21f, -717.71f),
                GatheringType = 2,
                NodeSet = 8
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35084,
                Position = new Vector3 (-671.18f, 93.37f, -819.39f),
                LandZone = new Vector3 (-670.57f, 92.57f, -819.02f),
                GatheringType = 2,
                NodeSet = 8
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35083,
                Position = new Vector3 (-679.34f, 91.67f, -804.68f),
                LandZone = new Vector3 (-678.57f, 90.89f, -804.33f),
                GatheringType = 2,
                NodeSet = 8
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35082,
                Position = new Vector3 (-752.37f, 88.51f, -717.92f),
                LandZone = new Vector3 (-751.92f, 87.59f, -717.87f),
                GatheringType = 2,
                NodeSet = 8
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35081,
                Position = new Vector3 (-758.07f, 88.73f, -707.39f),
                LandZone = new Vector3 (-757.45f, 87.93f, -707.14f),
                GatheringType = 2,
                NodeSet = 8
            },

            #endregion

            #region (Set #9)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35091,
                Position = new Vector3 (-642.11f, 69.73f, -572.83f),
                LandZone = new Vector3 (-641.41f, 68.81f, -572.6f),
                GatheringType = 2,
                NodeSet = 9
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35092,
                Position = new Vector3 (-652.4f, 71.72f, -564.69f),
                LandZone = new Vector3 (-652.33f, 70.89f, -564.22f),
                GatheringType = 2,
                NodeSet = 9
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35089,
                Position = new Vector3 (-731.63f, 79.66f, -509.58f),
                LandZone = new Vector3 (-731.27f, 78.7f, -509.72f),
                GatheringType = 2,
                NodeSet = 9
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35090,
                Position = new Vector3 (-727.81f, 79.13f, -503.01f),
                LandZone = new Vector3 (-727.68f, 78.05f, -502.8f),
                GatheringType = 2,
                NodeSet = 9
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35087,
                Position = new Vector3 (-640.96f, 60.56f, -463.86f),
                LandZone = new Vector3 (-640.66f, 59.77f, -463.8f),
                GatheringType = 2,
                NodeSet = 9
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35088,
                Position = new Vector3 (-637.53f, 59.94f, -456.5f),
                LandZone = new Vector3 (-637.1f, 58.91f, -456.96f),
                GatheringType = 2,
                NodeSet = 9
            },


            #endregion

        #endregion

        #region Botanist Nodes

            #region (Set #11)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35168,
                Position = new Vector3 (-228.04f, 17.31f, -13.59f),
                LandZone = new Vector3 (-228.84f, 16.82f, -13.53f),
                GatheringType = 3,
                NodeSet = 11
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35167,
                Position = new Vector3 (-238.16f, 17.82f, -33.92f),
                LandZone = new Vector3 (-237.94f, 17.22f, -33.02f),
                GatheringType = 3,
                NodeSet = 11
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35166,
                Position = new Vector3 (-325.44f, 28.14f, -51.83f),
                LandZone = new Vector3 (-324.69f, 27.81f, -51.55f),
                GatheringType = 3,
                NodeSet = 11
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35165,
                Position = new Vector3 (-330.45f, 29.18f, -28.82f),
                LandZone = new Vector3 (-330.08f, 28.69f, -29.41f),
                GatheringType = 3,
                NodeSet = 11
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35169,
                Position = new Vector3 (-288.95f, 25.49f, 44.38f),
                LandZone = new Vector3 (-288.93f, 25.07f, 43.86f),
                GatheringType = 3,
                NodeSet = 11
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35170,
                Position = new Vector3 (-273.81f, 23.78f, 44.75f),
                LandZone = new Vector3 (-274.15f, 23.28f, 44.57f),
                GatheringType = 3,
                NodeSet = 11
            },

            #endregion

            #region (Set #12)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35159,
                Position = new Vector3 (223.44f, 20.26f, 8.42f),
                LandZone = new Vector3 (223.04f, 19.41f, 8.25f),
                GatheringType = 3,
                NodeSet = 12
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35160,
                Position = new Vector3 (223.99f, 20.31f, 15.83f),
                LandZone = new Vector3 (224.33f, 19.39f, 15.45f),
                GatheringType = 3,
                NodeSet = 12
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35163,
                Position = new Vector3 (285.3f, 18.88f, 108.14f),
                LandZone = new Vector3 (285.2f, 17.89f, 108.78f),
                GatheringType = 3,
                NodeSet = 12
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35164,
                Position = new Vector3 (286.34f, 19.03f, 114.67f),
                LandZone = new Vector3 (286.04f, 18.02f, 114.53f),
                GatheringType = 3,
                NodeSet = 12
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35162,
                Position = new Vector3 (207.68f, 18.92f, 145.46f),
                LandZone = new Vector3 (207.74f, 17.93f, 145.63f),
                GatheringType = 3,
                NodeSet = 12
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35161,
                Position = new Vector3 (186.24f, 19.58f, 152.27f),
                LandZone = new Vector3 (186.34f, 18.68f, 152.56f),
                GatheringType = 3,
                NodeSet = 12
            },

            #endregion

            #region (Set #13)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35137,
                Position = new Vector3 (221.91f, 20.14f, 1.22f),
                LandZone = new Vector3 (221.63f, 19.27f, 1.76f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35136,
                Position = new Vector3 (214.32f, 20.07f, -9.12f),
                LandZone = new Vector3 (214.58f, 19.17f, -9.09f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35141,
                Position = new Vector3 (212.42f, 18.82f, -23.42f),
                LandZone = new Vector3 (212.86f, 18.07f, -22.78f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35140,
                Position = new Vector3 (231.74f, 19.9f, -27.72f),
                LandZone = new Vector3 (231.49f, 18.96f, -27.89f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35139,
                Position = new Vector3 (244.55f, 21.27f, -39.45f),
                LandZone = new Vector3 (244.63f, 20.4f, -39.56f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35138,
                Position = new Vector3 (251.86f, 22.5f, -54.28f),
                LandZone = new Vector3 (252.26f, 21.65f, -54.21f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35142,
                Position = new Vector3 (238.31f, 21.42f, -64.92f),
                LandZone = new Vector3 (238.52f, 20.47f, -65.07f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35135,
                Position = new Vector3 (225.6f, 20.73f, -83.05f),
                LandZone = new Vector3 (225.59f, 19.73f, -82.86f),
                GatheringType = 3,
                NodeSet = 13
            },

        #endregion

            #region (Set #14)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35143,
                Position = new Vector3 (421.1f, 33.1f, 189.18f),
                LandZone = new Vector3 (421.21f, 32.62f, 189.4f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35150,
                Position = new Vector3 (441.48f, 33.91f, 186.57f),
                LandZone = new Vector3 (441.11f, 33.22f, 186.48f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35147,
                Position = new Vector3 (439.68f, 34.32f, 211f),
                LandZone = new Vector3 (439.92f, 33.71f, 210.52f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35148,
                Position = new Vector3 (460.41f, 34.31f, 206.39f),
                LandZone = new Vector3 (460.12f, 33.59f, 206.25f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35149,
                Position = new Vector3 (468.54f, 34.92f, 216.16f),
                LandZone = new Vector3 (468.2f, 34.34f, 216.77f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35144,
                Position = new Vector3 (459.23f, 34.89f, 234.63f),
                LandZone = new Vector3 (459.71f, 34.33f, 234.41f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35145,
                Position = new Vector3 (454.67f, 34.75f, 254.14f),
                LandZone = new Vector3 (454.88f, 34.04f, 254.43f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35146,
                Position = new Vector3 (461.14f, 35.08f, 268.28f),
                LandZone = new Vector3 (461.07f, 34.47f, 267.78f),
                GatheringType = 3,
                NodeSet = 14
            },

            #endregion

            #region (Set #15)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35176,
                Position = new Vector3 (-185.84f, 32.88f, 351.45f),
                LandZone = new Vector3 (-185.52f, 32.05f, 351.6f),
                GatheringType = 3,
                NodeSet = 15
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35175,
                Position = new Vector3 (-174.21f, 32.41f, 342.16f),
                LandZone = new Vector3 (-174.13f, 31.51f, 342.22f),
                GatheringType = 3,
                NodeSet = 15
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35171,
                Position = new Vector3 (-83.13f, 28.26f, 312.21f),
                LandZone = new Vector3 (-83.36f, 27.38f, 311.6f),
                GatheringType = 3,
                NodeSet = 15
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35172,
                Position = new Vector3 (-69.83f, 27.94f, 330.99f),
                LandZone = new Vector3 (-69.73f, 27.02f, 330.61f),
                GatheringType = 3,
                NodeSet = 15
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35173,
                Position = new Vector3 (-110.16f, 34.36f, 427.53f),
                LandZone = new Vector3 (-110.27f, 33.45f, 428.06f),
                GatheringType = 3,
                NodeSet = 15
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35174,
                Position = new Vector3 (-116.48f, 34.47f, 438.21f),
                LandZone = new Vector3 (-116.7f, 33.51f, 437.75f),
                GatheringType = 3,
                NodeSet = 15
            },

            #endregion

            #region (Set #16)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35177,
                Position = new Vector3 (490.19f, 36.57f, 174.08f),
                LandZone = new Vector3 (489.71f, 35.7f, 174.19f),
                GatheringType = 3,
                NodeSet = 16
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35178,
                Position = new Vector3 (441.94f, 36.48f, 176.07f),
                LandZone = new Vector3 (441.74f, 35.5f, 176.29f),
                GatheringType = 3,
                NodeSet = 16
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35180,
                Position = new Vector3 (382.5f, 35.42f, 265.39f),
                LandZone = new Vector3 (383.06f, 35.01f, 264.97f),
                GatheringType = 3,
                NodeSet = 16
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35179,
                Position = new Vector3 (395.4f, 37.31f, 276.67f),
                LandZone = new Vector3 (395.65f, 36.48f, 276.62f),
                GatheringType = 3,
                NodeSet = 16
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35181,
                Position = new Vector3 (476.83f, 40.68f, 297.1f),
                LandZone = new Vector3 (477.28f, 39.93f, 297.15f),
                GatheringType = 3,
                NodeSet = 16
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35182,
                Position = new Vector3 (510.84f, 37.4f, 283.41f),
                LandZone = new Vector3 (510.55f, 36.63f, 283.3f),
                GatheringType = 3,
                NodeSet = 16
            },

            #endregion

            #region (Set #17)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35151,
                Position = new Vector3 (559.91f, 55.79f, 672.96f),
                LandZone = new Vector3 (559.79f, 55.21f, 672.69f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35158,
                Position = new Vector3 (536.1f, 55.63f, 679.65f),
                LandZone = new Vector3 (536.37f, 54.9f, 679.87f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35154,
                Position = new Vector3 (520.14f, 56.14f, 694.22f),
                LandZone = new Vector3 (520.91f, 55.61f, 693.97f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35155,
                Position = new Vector3 (502.04f, 56.63f, 680.56f),
                LandZone = new Vector3 (502.08f, 55.68f, 680.68f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35157,
                Position = new Vector3 (481.21f, 56.12f, 660.01f),
                LandZone = new Vector3 (481.17f, 55.23f, 660.35f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35156,
                Position = new Vector3 (489.9f, 56.31f, 671.44f),
                LandZone = new Vector3 (490.2f, 55.49f, 671.69f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35152,
                Position = new Vector3 (464.35f, 55.87f, 661.57f),
                LandZone = new Vector3 (464.23f, 55.16f, 661.27f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35153,
                Position = new Vector3 (452.62f, 56.02f, 663.78f),
                LandZone = new Vector3 (452.79f, 55.22f, 663.5f),
                GatheringType = 3,
                NodeSet = 17
            },

            #endregion

            #region (Set #18)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35183,
                Position = new Vector3 (522.22f, 53.2f, 514.22f),
                LandZone = new Vector3 (522.16f, 52.29f, 515.22f),
                GatheringType = 3,
                NodeSet = 18
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35184,
                Position = new Vector3 (529.68f, 54.04f, 488.3f),
                LandZone = new Vector3 (529.51f, 53.32f, 488.7f),
                GatheringType = 3,
                NodeSet = 18
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35185,
                Position = new Vector3 (579.7f, 54.15f, 412.27f),
                LandZone = new Vector3 (579.61f, 53.31f, 412.66f),
                GatheringType = 3,
                NodeSet = 18
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35186,
                Position = new Vector3 (592.29f, 54.46f, 429.05f),
                LandZone = new Vector3 (592.12f, 53.71f, 428.87f),
                GatheringType = 3,
                NodeSet = 18
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35187,
                Position = new Vector3 (671.4f, 64.6f, 498.43f),
                LandZone = new Vector3 (670.72f, 63.73f, 498.07f),
                GatheringType = 3,
                NodeSet = 18
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35188,
                Position = new Vector3 (651.13f, 57.95f, 522.03f),
                LandZone = new Vector3 (651.07f, 57.13f, 521.38f),
                GatheringType = 3,
                NodeSet = 18
            },

            #endregion

            #region (Set #19)

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35194,
                Position = new Vector3 (595.76f, 58.28f, 648.99f),
                LandZone = new Vector3 (595.16f, 57.34f, 650.08f),
                GatheringType = 3,
                NodeSet = 19
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35193,
                Position = new Vector3 (602.08f, 59.59f, 659.02f),
                LandZone = new Vector3 (601.25f, 58.52f, 658.62f),
                GatheringType = 3,
                NodeSet = 19
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35192,
                Position = new Vector3 (484.43f, 56.59f, 676.53f),
                LandZone = new Vector3 (484.55f, 55.62f, 676.34f),
                GatheringType = 3,
                NodeSet = 19
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35191,
                Position = new Vector3 (463.9f, 55.91f, 671.28f),
                LandZone = new Vector3 (464.47f, 55.17f, 670.95f),
                GatheringType = 3,
                NodeSet = 19
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35190,
                Position = new Vector3 (468.77f, 55.87f, 591.9f),
                LandZone = new Vector3 (469.39f, 55.08f, 592.1f),
                GatheringType = 3,
                NodeSet = 19
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35189,
                Position = new Vector3 (492.08f, 54.21f, 574.35f),
                LandZone = new Vector3 (491.81f, 53.34f, 574.8f),
                GatheringType = 3,
                NodeSet = 19
            },

            #endregion

            #region (Set #20)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35196,
            Position = new Vector3 (-684.35f, 58.86f, 503.32f),
            LandZone = new Vector3 (-684.05f, 58.24f, 503.64f),
            GatheringType = 3,
            NodeSet = 20
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35195,
            Position = new Vector3 (-701.75f, 62.75f, 512.91f),
            LandZone = new Vector3 (-701.3f, 62.17f, 513.41f),
            GatheringType = 3,
            NodeSet = 20
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35200,
            Position = new Vector3 (-772.7f, 69.54f, 593.97f),
            LandZone = new Vector3 (-773.43f, 68.96f, 593.82f),
            GatheringType = 3,
            NodeSet = 20
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35199,
            Position = new Vector3 (-770.47f, 70.37f, 608.22f),
            LandZone = new Vector3 (-769.76f, 69.6f, 608.24f),
            GatheringType = 3,
            NodeSet = 20
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35198,
            Position = new Vector3 (-651.49f, 56.65f, 620.18f),
            LandZone = new Vector3 (-651.85f, 56.27f, 619.56f),
            GatheringType = 3,
            NodeSet = 20
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35197,
            Position = new Vector3 (-649.1f, 57.32f, 604.2f),
            LandZone = new Vector3 (-649.86f, 57.03f, 604.38f),
            GatheringType = 3,
            NodeSet = 20
        },

        #endregion

        #endregion

        #region Botanist Criticals

                new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 0,
                    Position = new Vector3 (),
                    LandZone = new Vector3 (),
                    GatheringType = 2,
                    NodeSet = 24
                },
                new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 0,
                    Position = new Vector3 (),
                    LandZone = new Vector3 (),
                    GatheringType = 2,
                    NodeSet = 24
                },
                new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 0,
                    Position = new Vector3 (),
                    LandZone = new Vector3 (),
                    GatheringType = 2,
                    NodeSet = 24
                },
                new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 0,
                    Position = new Vector3 (),
                    LandZone = new Vector3 (),
                    GatheringType = 2,
                    NodeSet = 24
                },
                new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 0,
                    Position = new Vector3 (),
                    LandZone = new Vector3 (),
                    GatheringType = 2,
                    NodeSet = 24
                },
                new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 0,
                    Position = new Vector3 (),
                    LandZone = new Vector3 (),
                    GatheringType = 2,
                    NodeSet = 24
                },


        #region BTN Critical #25

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35225,
                Position = new Vector3(178.5146f, 28.33517f, -234.279f),
                LandZone = new Vector3(178.5146f, 28.33517f, -234.279f),
                GatheringType = 3,
                NodeSet = 25
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35226,
                Position = new Vector3(197.6805f, 31.65541f, -243.8829f),
                LandZone = new Vector3(197.6805f, 31.65541f, -243.8829f),
                GatheringType = 3,
                NodeSet = 25
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35230,
                Position = new Vector3(144.964f, 22.65566f, -198.4546f),
                LandZone = new Vector3(144.964f, 22.65566f, -198.4546f),
                GatheringType = 3,
                NodeSet = 25
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35229,
                Position = new Vector3(167.5034f, 24.00558f, -186.3173f),
                LandZone = new Vector3(167.5034f, 24.00558f, -186.3173f),
                GatheringType = 3,
                NodeSet = 25
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35228,
                Position = new Vector3(230.1051f, 26.79718f, -167.2313f),
                LandZone = new Vector3(230.1051f, 26.79718f, -167.2313f),
                GatheringType = 3,
                NodeSet = 25
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35227,
                Position = new Vector3(234.4044f, 27.3815f, -175.0399f),
                LandZone = new Vector3(234.4044f, 27.3815f, -175.0399f),
                GatheringType = 3,
                NodeSet = 25
            },

            #endregion

        #region BTN Critical #26

            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35213,
                Position = new Vector3(778.8542f, 58.51204f, 90.23239f),
                LandZone = new Vector3(778.8542f, 58.51204f, 90.23239f),
                GatheringType = 3,
                NodeSet = 26
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35214,
                Position = new Vector3(785.6716f, 59.13748f, 69.10181f),
                LandZone = new Vector3(785.6716f, 59.13748f, 69.10181f),
                GatheringType = 3,
                NodeSet = 26
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35215,
                Position = new Vector3(724.3608f, 56.97183f, 149.7798f),
                LandZone = new Vector3(724.3608f, 56.97183f, 149.7798f),
                GatheringType = 3,
                NodeSet = 26
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35216,
                Position = new Vector3(733.382f, 57.84801f, 152.2305f),
                LandZone = new Vector3(733.382f, 57.84801f, 152.2305f),
                GatheringType = 3,
                NodeSet = 26
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35217,
                Position = new Vector3(710.4752f, 53.412f, 49.43619f),
                LandZone = new Vector3(710.4752f, 53.412f, 49.43619f),
                GatheringType = 3,
                NodeSet = 26
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35218,
                Position = new Vector3(700.0176f, 52.94641f, 56.65482f),
                LandZone = new Vector3(700.0176f, 52.94641f, 56.65482f),
                GatheringType = 3,
                NodeSet = 26
            },

            #endregion

        #endregion

        #region Miner Criticals

        // Miner
        #region Miner Critical #21

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35115,
            Position = new Vector3(-501.869f, 48.78254f, -376.7437f),
            LandZone = new Vector3(-501.869f, 48.78254f, -376.7437f),
            GatheringType = 2,
            NodeSet = 21
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35116,
            Position = new Vector3(-518.8838f, 48.09315f, -370.4776f),
            LandZone = new Vector3(-518.8838f, 48.09315f, -370.4776f),
            GatheringType = 2,
            NodeSet = 21
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35112,
            Position = new Vector3(-541.9754f, 48.69396f, -303.8165f),
            LandZone = new Vector3(-541.9754f, 48.69396f, -303.8165f),
            GatheringType = 2,
            NodeSet = 21
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35111,
            Position = new Vector3(-544.8497f, 46.38572f, -288.653f),
            LandZone = new Vector3(-544.8497f, 46.38572f, -288.653f),
            GatheringType = 2,
            NodeSet = 21
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35114,
            Position = new Vector3(-453.9398f, 43.02265f, -287.6784f),
            LandZone = new Vector3(-453.9398f, 43.02265f, -287.6784f),
            GatheringType = 2,
            NodeSet = 21
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35113,
            Position = new Vector3(-459.9001f, 44.85727f, -304.8606f),
            LandZone = new Vector3(-459.9001f, 44.85727f, -304.8606f),
            GatheringType = 2,
            NodeSet = 21
        },

        #endregion

        #region Miner Critical #22

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35118,
            Position = new Vector3(-88.08077f, 22.78399f, -356.0785f),
            LandZone = new Vector3(-88.08077f, 22.78399f, -356.0785f),
            GatheringType = 2,
            NodeSet = 22
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35117,
            Position = new Vector3(-91.31516f, 22.02787f, -345.187f),
            LandZone = new Vector3(-91.31516f, 22.02787f, -345.187f),
            GatheringType = 2,
            NodeSet = 22
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35119,
            Position = new Vector3(-127.8303f, 30.40603f, -423.2329f),
            LandZone = new Vector3(-127.8303f, 30.40603f, -423.2329f),
            GatheringType = 2,
            NodeSet = 22
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35120,
            Position = new Vector3(-146.9514f, 29.88647f, -412.1004f),
            LandZone = new Vector3(-146.9514f, 29.88647f, -412.1004f),
            GatheringType = 2,
            NodeSet = 22
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35121,
            Position = new Vector3(-160.7067f, 25.29499f, -317.5167f),
            LandZone = new Vector3(-160.7067f, 25.29499f, -317.5167f),
            GatheringType = 2,
            NodeSet = 22
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35122,
            Position = new Vector3(-150.4397f, 23.76383f, -313.2321f),
            LandZone = new Vector3(-150.4397f, 23.76383f, -313.2321f),
            GatheringType = 2,
            NodeSet = 22
        },

        #endregion

        #region Miner Critical #23

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35132,
            Position = new Vector3(-314.1873f, 27.93501f, 121.8422f),
            LandZone = new Vector3(-314.1873f, 27.93501f, 121.8422f),
            GatheringType = 2,
            NodeSet = 23
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35133,
            Position = new Vector3(-317.8292f, 28.43479f, 135.2601f),
            LandZone = new Vector3(-317.8292f, 28.43479f, 135.2601f),
            GatheringType = 2,
            NodeSet = 23
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35134,
            Position = new Vector3(-277.699f, 24.79578f, 187.4265f),
            LandZone = new Vector3(-277.699f, 24.79578f, 187.4265f),
            GatheringType = 2,
            NodeSet = 23
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35131,
            Position = new Vector3(-245.4429f, 24.62878f, 188.8734f),
            LandZone = new Vector3(-245.4429f, 24.62878f, 188.8734f),
            GatheringType = 2,
            NodeSet = 23
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35129,
            Position = new Vector3(-216.6629f, 19.97406f, 105.9128f),
            LandZone = new Vector3(-216.6629f, 19.97406f, 105.9128f),
            GatheringType = 2,
            NodeSet = 23
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35130,
            Position = new Vector3(-243.6713f, 20.00452f, 90.37915f),
            LandZone = new Vector3(-243.6713f, 20.00452f, 90.37915f),
            GatheringType = 2,
            NodeSet = 23
        },

        #endregion

        // Botanist

        #endregion

    };

    public static Dictionary<Vector2, List<GathNodeInfo>> NodeSetV2 = new()
    {
        // - - - - - - - - - - - 
        // Miner
        // - - - - - - - - - - - 

        // Miner | 8 nodes | #1
        { new Vector2(-119, -175), new List<GathNodeInfo>() {
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35040,
                    Position = new Vector3 (-50.68f, 19.41f, -208.97f),
                    LandZone = new Vector3 (-50.64f, 18.41f, -209.93f),
                    GatheringType = 2,
                    NodeSet = 1
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35039,
                    Position = new Vector3 (-73.1f, 20.16f, -204.29f),
                    LandZone = new Vector3 (-73.45f, 19.12f, -205.25f),
                    GatheringType = 2,
                    NodeSet = 1
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35038,
                    Position = new Vector3 (-90.95f, 22.52f, -194.11f),
                    LandZone = new Vector3 (-91.03f, 21.15f, -194.67f),
                    GatheringType = 2,
                    NodeSet = 1
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35037,
                    Position = new Vector3 (-109.77f, 24.82f, -187.37f),
                    LandZone = new Vector3 (-109.83f, 23.74f, -188.08f),
                    GatheringType = 2,
                    NodeSet = 1
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35036,
                    Position = new Vector3 (-129.53f, 27.94f, -170.34f),
                    LandZone = new Vector3 (-129.85f, 26.59f, -170.81f),
                    GatheringType = 2,
                    NodeSet = 1
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35035,
                    Position = new Vector3 (-135.65f, 28.2f, -156.82f),
                    LandZone = new Vector3 (-135.94f, 27f, -157.07f),
                    GatheringType = 2,
                    NodeSet = 1
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35034,
                    Position = new Vector3 (-142.32f, 27.14f, -139.7f),
                    LandZone = new Vector3 (-142.57f, 25.67f, -140.04f),
                    GatheringType = 2,
                    NodeSet = 1
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35033,
                    Position = new Vector3 (-153.94f, 24.31f, -124.47f),
                    LandZone = new Vector3 (-154.36f, 23.29f, -124.61f),
                    GatheringType = 2,
                    NodeSet = 1
                },
        } },
        // Miner | 6 Nodes | #2
        { new Vector2(-168, -181), new List<GathNodeInfo>() {
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35062,
                    Position = new Vector3 (-110.86f, 20.37f, -226.34f),
                    LandZone = new Vector3 (-110.73f, 19.3f, -225.31f),
                    GatheringType = 2,
                    NodeSet = 2
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35061,
                    Position = new Vector3 (-117.08f, 20.9f, -230.27f),
                    LandZone = new Vector3 (-118.01f, 20f, -230.48f),
                    GatheringType = 2,
                    NodeSet = 2
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35057,
                    Position = new Vector3 (-226.12f, 29.29f, -176.71f),
                    LandZone = new Vector3 (-225.34f, 28.75f, -176.68f),
                    GatheringType = 2,
                    NodeSet = 2
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35058,
                    Position = new Vector3 (-228.37f, 27.66f, -167.48f),
                    LandZone = new Vector3 (-227.69f, 27.39f, -167.57f),
                    GatheringType = 2,
                    NodeSet = 2
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35059,
                    Position = new Vector3 (-170.4f, 20.94f, -116.41f),
                    LandZone = new Vector3 (-170.99f, 20.28f, -116.77f),
                    GatheringType = 2,
                    NodeSet = 2
                },
            new GathNodeInfo
                {
                    ZoneId = 1237,
                    NodeId = 35060,
                    Position = new Vector3 (-162.69f, 22.16f, -124.16f),
                    LandZone = new Vector3 (-162.84f, 21.93f, -125.09f),
                    GatheringType = 2,
                    NodeSet = 2
                },
        } },
        // Miner | 6 Nodes | #3
        { new Vector2(96, 259), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35066,
                Position = new Vector3 (60.94f, 22.63f, 204.94f),
                LandZone = new Vector3 (60.95f, 21.53f, 205.38f),
                GatheringType = 2,
                NodeSet = 3
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35065,
                Position = new Vector3 (40.95f, 21.06f, 208.7f),
                LandZone = new Vector3 (41.39f, 19.97f, 209.17f),
                GatheringType = 2,
                NodeSet = 3
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35063,
                Position = new Vector3 (65.52f, 18.94f, 318.76f),
                LandZone = new Vector3 (65.6f, 18.21f, 318.18f),
                GatheringType = 2,
                NodeSet = 3
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35064,
                Position = new Vector3 (75.12f, 19.39f, 322.87f),
                LandZone = new Vector3 (75.45f, 18.57f, 322.06f),
                GatheringType = 2,
                NodeSet = 3
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35068,
                Position = new Vector3 (172.32f, 23.64f, 275.08f),
                LandZone = new Vector3 (172.16f, 22.95f, 274.95f),
                GatheringType = 2,
                NodeSet = 3
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35067,
                Position = new Vector3 (174.37f, 23.41f, 267.84f),
                LandZone = new Vector3 (174.24f, 22.82f, 267.61f),
                GatheringType = 2,
                NodeSet = 3
            },
        } },
        // Miner | 8 Nodes | #4
        { new Vector2(65, -431), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35041,
                Position = new Vector3 (70.23f, 35.63f, -370.83f),
                LandZone = new Vector3 (69.72f, 35.02f, -370.42f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35042,
                Position = new Vector3 (56.98f, 36.75f, -385.37f),
                LandZone = new Vector3 (57.49f, 35.95f, -384.87f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35043,
                Position = new Vector3 (78.47f, 39.5f, -424.33f),
                LandZone = new Vector3 (77.97f, 39.06f, -424.98f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35044,
                Position = new Vector3 (56.85f, 40.02f, -444.27f),
                LandZone = new Vector3 (57.26f, 39.27f, -444f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35045,
                Position = new Vector3 (56.59f, 40.24f, -453.96f),
                LandZone = new Vector3 (57.03f, 39.6f, -454.24f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35046,
                Position = new Vector3 (45.23f, 41.43f, -473.65f),
                LandZone = new Vector3 (45.55f, 40.41f, -473.4f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35047,
                Position = new Vector3 (49.7f, 41.83f, -481.26f),
                LandZone = new Vector3 (50.06f, 40.96f, -481.57f),
                GatheringType = 2,
                NodeSet = 4
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35048,
                Position = new Vector3 (60.49f, 43.1f, -499.58f),
                LandZone = new Vector3 (60.52f, 42.33f, -499.64f),
                GatheringType = 2,
                NodeSet = 4
            },
        } },
        // Miner | 6 Nodes | #5
        { new Vector2(-475, 135), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35072,
                Position = new Vector3 (-430.47f, 42.53f, 96.48f),
                LandZone = new Vector3 (-430.96f, 42.04f, 96.05f),
                GatheringType = 2,
                NodeSet = 5
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35071,
                Position = new Vector3 (-438.91f, 43.87f, 101.09f),
                LandZone = new Vector3 (-438.25f, 42.71f, 101.14f),
                GatheringType = 2,
                NodeSet = 5
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35074,
                Position = new Vector3 (-543.83f, 41.39f, 78.79f),
                LandZone = new Vector3 (-543.35f, 40.24f, 78.7f),
                GatheringType = 2,
                NodeSet = 5
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35073,
                Position = new Vector3 (-542.48f, 44.36f, 91.48f),
                LandZone = new Vector3 (-542.08f, 43.15f, 91.97f),
                GatheringType = 2,
                NodeSet = 5
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35070,
                Position = new Vector3 (-400.24f, 45f, 191.3f),
                LandZone = new Vector3 (-400.94f, 44.26f, 190.28f),
                GatheringType = 2,
                NodeSet = 5
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35069,
                Position = new Vector3 (-394.25f, 44.13f, 189.71f),
                LandZone = new Vector3 (-394.35f, 43.28f, 188.94f),
                GatheringType = 2,
                NodeSet = 5
            },
        } },
        // Miner | 6 Nodes | #6
        { new Vector2(73, -482), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35078,
                Position = new Vector3 (77.56f, 39.48f, -424f),
                LandZone = new Vector3 (76.97f, 38.91f, -424.55f),
                GatheringType = 2,
                NodeSet = 6
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35077,
                Position = new Vector3 (87.1f, 39.77f, -424.88f),
                LandZone = new Vector3 (87.04f, 39.23f, -425.47f),
                GatheringType = 2,
                NodeSet = 6
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35076,
                Position = new Vector3 (142.59f, 47.46f, -491.28f),
                LandZone = new Vector3 (142.78f, 46.57f, -490.84f),
                GatheringType = 2,
                NodeSet = 6
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35075,
                Position = new Vector3 (135.91f, 47.36f, -500.54f),
                LandZone = new Vector3 (135.63f, 46.7f, -500.5f),
                GatheringType = 2,
                NodeSet = 6
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35079,
                Position = new Vector3 (32.70f, 43.50f, -521.08f),
                LandZone = new Vector3 (32.70f, 43.50f, -521.08f),
                GatheringType = 2,
                NodeSet = 6
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35080,
                Position = new Vector3 (15.94f, 44.39f, -526.71f),
                LandZone = new Vector3 (15.66f, 43.36f, -526.49f),
                GatheringType = 2,
                NodeSet = 6
            },
        } },
        // Miner | 8 Nodes | #7
        { new Vector2(-463, -729), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35056,
                Position = new Vector3 (-419.94f, 66.8f, -692.3f),
                LandZone = new Vector3 (-420.07f, 66.15f, -691.43f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35055,
                Position = new Vector3 (-428.39f, 67.89f, -704.01f),
                LandZone = new Vector3 (-428.84f, 67.15f, -703.4f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35054,
                Position = new Vector3 (-447.28f, 68.51f, -707.15f),
                LandZone = new Vector3 (-446.41f, 67.61f, -707.17f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35053,
                Position = new Vector3 (-461.66f, 69.71f, -713.83f),
                LandZone = new Vector3 (-462.18f, 68.99f, -713.69f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35052,
                Position = new Vector3 (-462.91f, 71.27f, -731.6f),
                LandZone = new Vector3 (-463.43f, 70.55f, -731.26f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35051,
                Position = new Vector3 (-467.54f, 73.48f, -747.74f),
                LandZone = new Vector3 (-468f, 72.64f, -747.89f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35050,
                Position = new Vector3 (-469.04f, 76.77f, -770.29f),
                LandZone = new Vector3 (-469.29f, 76.02f, -769.88f),
                GatheringType = 2,
                NodeSet = 7
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35049,
                Position = new Vector3 (-492.64f, 78.8f, -777f),
                LandZone = new Vector3 (-492.64f, 78.01f, -776.5f),
                GatheringType = 2,
                NodeSet = 7
            },
        } },
        // Miner | 6 Nodes | #8
        { new Vector2(-690, -752), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35086,
                Position = new Vector3 (-635.22f, 73.97f, -704.67f),
                LandZone = new Vector3 (-636.22f, 73.17f, -703.98f),
                GatheringType = 2,
                NodeSet = 8
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35085,
                Position = new Vector3 (-621.59f, 75.08f, -715.89f),
                LandZone = new Vector3 (-620.05f, 74.21f, -717.71f),
                GatheringType = 2,
                NodeSet = 8
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35084,
                Position = new Vector3 (-671.18f, 93.37f, -819.39f),
                LandZone = new Vector3 (-670.57f, 92.57f, -819.02f),
                GatheringType = 2,
                NodeSet = 8
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35083,
                Position = new Vector3 (-679.34f, 91.67f, -804.68f),
                LandZone = new Vector3 (-678.57f, 90.89f, -804.33f),
                GatheringType = 2,
                NodeSet = 8
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35082,
                Position = new Vector3 (-752.37f, 88.51f, -717.92f),
                LandZone = new Vector3 (-751.92f, 87.59f, -717.87f),
                GatheringType = 2,
                NodeSet = 8
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35081,
                Position = new Vector3 (-758.07f, 88.73f, -707.39f),
                LandZone = new Vector3 (-757.45f, 87.93f, -707.14f),
                GatheringType = 2,
                NodeSet = 8
            },
        } },
        // Miner | x Nodes | #9
        { new Vector2(-669, -515), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35091,
                Position = new Vector3 (-642.11f, 69.73f, -572.83f),
                LandZone = new Vector3 (-641.41f, 68.81f, -572.6f),
                GatheringType = 2,
                NodeSet = 9
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35092,
                Position = new Vector3 (-652.4f, 71.72f, -564.69f),
                LandZone = new Vector3 (-652.33f, 70.89f, -564.22f),
                GatheringType = 2,
                NodeSet = 9
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35089,
                Position = new Vector3 (-731.63f, 79.66f, -509.58f),
                LandZone = new Vector3 (-731.27f, 78.7f, -509.72f),
                GatheringType = 2,
                NodeSet = 9
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35090,
                Position = new Vector3 (-727.81f, 79.13f, -503.01f),
                LandZone = new Vector3 (-727.68f, 78.05f, -502.8f),
                GatheringType = 2,
                NodeSet = 9
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35087,
                Position = new Vector3 (-640.96f, 60.56f, -463.86f),
                LandZone = new Vector3 (-640.66f, 59.77f, -463.8f),
                GatheringType = 2,
                NodeSet = 9
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35088,
                Position = new Vector3 (-637.53f, 59.94f, -456.5f),
                LandZone = new Vector3 (-637.1f, 58.91f, -456.96f),
                GatheringType = 2,
                NodeSet = 9
            },
        } },

        // Criticals -
        // Miner | Critial #1
        { new Vector2(-503, -324), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35115,
                Position = new Vector3(-501.869f, 48.78254f, -376.7437f),
                LandZone = new Vector3(-501.869f, 48.78254f, -376.7437f),
                GatheringType = 2,
                NodeSet = 21
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35116,
                Position = new Vector3(-518.8838f, 48.09315f, -370.4776f),
                LandZone = new Vector3(-518.8838f, 48.09315f, -370.4776f),
                GatheringType = 2,
                NodeSet = 21
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35112,
                Position = new Vector3(-541.9754f, 48.69396f, -303.8165f),
                LandZone = new Vector3(-541.9754f, 48.69396f, -303.8165f),
                GatheringType = 2,
                NodeSet = 21
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35111,
                Position = new Vector3(-544.8497f, 46.38572f, -288.653f),
                LandZone = new Vector3(-544.8497f, 46.38572f, -288.653f),
                GatheringType = 2,
                NodeSet = 21
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35114,
                Position = new Vector3(-453.9398f, 43.02265f, -287.6784f),
                LandZone = new Vector3(-453.9398f, 43.02265f, -287.6784f),
                GatheringType = 2,
                NodeSet = 21
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35113,
                Position = new Vector3(-459.9001f, 44.85727f, -304.8606f),
                LandZone = new Vector3(-459.9001f, 44.85727f, -304.8606f),
                GatheringType = 2,
                NodeSet = 21
            },
        } },
        // Miner | Critial #2
        { new Vector2(-131, -365), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35118,
                Position = new Vector3(-88.08077f, 22.78399f, -356.0785f),
                LandZone = new Vector3(-88.08077f, 22.78399f, -356.0785f),
                GatheringType = 2,
                NodeSet = 22
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35117,
                Position = new Vector3(-91.31516f, 22.02787f, -345.187f),
                LandZone = new Vector3(-91.31516f, 22.02787f, -345.187f),
                GatheringType = 2,
                NodeSet = 22
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35119,
                Position = new Vector3(-127.8303f, 30.40603f, -423.2329f),
                LandZone = new Vector3(-127.8303f, 30.40603f, -423.2329f),
                GatheringType = 2,
                NodeSet = 22
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35120,
                Position = new Vector3(-146.9514f, 29.88647f, -412.1004f),
                LandZone = new Vector3(-146.9514f, 29.88647f, -412.1004f),
                GatheringType = 2,
                NodeSet = 22
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35121,
                Position = new Vector3(-160.7067f, 25.29499f, -317.5167f),
                LandZone = new Vector3(-160.7067f, 25.29499f, -317.5167f),
                GatheringType = 2,
                NodeSet = 22
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35122,
                Position = new Vector3(-150.4397f, 23.76383f, -313.2321f),
                LandZone = new Vector3(-150.4397f, 23.76383f, -313.2321f),
                GatheringType = 2,
                NodeSet = 22
            },
        } },
        // Miner | Critical #3
        { new Vector2(-270, 140), new List < GathNodeInfo >() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35132,
                Position = new Vector3(-314.1873f, 27.93501f, 121.8422f),
                LandZone = new Vector3(-314.1873f, 27.93501f, 121.8422f),
                GatheringType = 2,
                NodeSet = 23
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35133,
                Position = new Vector3(-317.8292f, 28.43479f, 135.2601f),
                LandZone = new Vector3(-317.8292f, 28.43479f, 135.2601f),
                GatheringType = 2,
                NodeSet = 23
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35134,
                Position = new Vector3(-277.699f, 24.79578f, 187.4265f),
                LandZone = new Vector3(-277.699f, 24.79578f, 187.4265f),
                GatheringType = 2,
                NodeSet = 23
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35131,
                Position = new Vector3(-245.4429f, 24.62878f, 188.8734f),
                LandZone = new Vector3(-245.4429f, 24.62878f, 188.8734f),
                GatheringType = 2,
                NodeSet = 23
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35129,
                Position = new Vector3(-216.6629f, 19.97406f, 105.9128f),
                LandZone = new Vector3(-216.6629f, 19.97406f, 105.9128f),
                GatheringType = 2,
                NodeSet = 23
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35130,
                Position = new Vector3(-243.6713f, 20.00452f, 90.37915f),
                LandZone = new Vector3(-243.6713f, 20.00452f, 90.37915f),
                GatheringType = 2,
                NodeSet = 23
            },
        } },

        // - - - - - - - - - - 
        // Botanist
        // - - - - - - - - - - 

        // Botanist | 6 Nodes | #1
        { new Vector2(-278, -13), new List<GathNodeInfo>(){
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35168,
                Position = new Vector3 (-228.04f, 17.31f, -13.59f),
                LandZone = new Vector3 (-228.84f, 16.82f, -13.53f),
                GatheringType = 3,
                NodeSet = 11
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35167,
                Position = new Vector3 (-238.16f, 17.82f, -33.92f),
                LandZone = new Vector3 (-237.94f, 17.22f, -33.02f),
                GatheringType = 3,
                NodeSet = 11
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35166,
                Position = new Vector3 (-325.44f, 28.14f, -51.83f),
                LandZone = new Vector3 (-324.69f, 27.81f, -51.55f),
                GatheringType = 3,
                NodeSet = 11
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35165,
                Position = new Vector3 (-330.45f, 29.18f, -28.82f),
                LandZone = new Vector3 (-330.08f, 28.69f, -29.41f),
                GatheringType = 3,
                NodeSet = 11
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35169,
                Position = new Vector3 (-288.95f, 25.49f, 44.38f),
                LandZone = new Vector3 (-288.93f, 25.07f, 43.86f),
                GatheringType = 3,
                NodeSet = 11
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35170,
                Position = new Vector3 (-273.81f, 23.78f, 44.75f),
                LandZone = new Vector3 (-274.15f, 23.28f, 44.57f),
                GatheringType = 3,
                NodeSet = 11
            },
        } },
        // Botanist | 6 Nodes | #2
        { new Vector2(225, 83), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35159,
                Position = new Vector3 (223.44f, 20.26f, 8.42f),
                LandZone = new Vector3 (223.04f, 19.41f, 8.25f),
                GatheringType = 3,
                NodeSet = 12
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35160,
                Position = new Vector3 (223.99f, 20.31f, 15.83f),
                LandZone = new Vector3 (224.33f, 19.39f, 15.45f),
                GatheringType = 3,
                NodeSet = 12
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35163,
                Position = new Vector3 (285.3f, 18.88f, 108.14f),
                LandZone = new Vector3 (285.2f, 17.89f, 108.78f),
                GatheringType = 3,
                NodeSet = 12
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35164,
                Position = new Vector3 (286.34f, 19.03f, 114.67f),
                LandZone = new Vector3 (286.04f, 18.02f, 114.53f),
                GatheringType = 3,
                NodeSet = 12
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35162,
                Position = new Vector3 (207.68f, 18.92f, 145.46f),
                LandZone = new Vector3 (207.74f, 17.93f, 145.63f),
                GatheringType = 3,
                NodeSet = 12
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35161,
                Position = new Vector3 (186.24f, 19.58f, 152.27f),
                LandZone = new Vector3 (186.34f, 18.68f, 152.56f),
                GatheringType = 3,
                NodeSet = 12
            },
        } },
        // Botanist | 8 Nodes | #3
        { new Vector2(232, -50), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35137,
                Position = new Vector3 (221.91f, 20.14f, 1.22f),
                LandZone = new Vector3 (221.63f, 19.27f, 1.76f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35136,
                Position = new Vector3 (214.32f, 20.07f, -9.12f),
                LandZone = new Vector3 (214.58f, 19.17f, -9.09f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35141,
                Position = new Vector3 (212.42f, 18.82f, -23.42f),
                LandZone = new Vector3 (212.86f, 18.07f, -22.78f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35140,
                Position = new Vector3 (231.74f, 19.9f, -27.72f),
                LandZone = new Vector3 (231.49f, 18.96f, -27.89f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35139,
                Position = new Vector3 (244.55f, 21.27f, -39.45f),
                LandZone = new Vector3 (244.63f, 20.4f, -39.56f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35138,
                Position = new Vector3 (251.86f, 22.5f, -54.28f),
                LandZone = new Vector3 (252.26f, 21.65f, -54.21f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35142,
                Position = new Vector3 (238.31f, 21.42f, -64.92f),
                LandZone = new Vector3 (238.52f, 20.47f, -65.07f),
                GatheringType = 3,
                NodeSet = 13
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35135,
                Position = new Vector3 (225.6f, 20.73f, -83.05f),
                LandZone = new Vector3 (225.59f, 19.73f, -82.86f),
                GatheringType = 3,
                NodeSet = 13
            },
        } },
        // Botanist | 8 Nodes | #4 
        { new Vector2(456, 221), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35143,
                Position = new Vector3 (421.1f, 33.1f, 189.18f),
                LandZone = new Vector3 (421.21f, 32.62f, 189.4f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35150,
                Position = new Vector3 (441.48f, 33.91f, 186.57f),
                LandZone = new Vector3 (441.11f, 33.22f, 186.48f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35147,
                Position = new Vector3 (439.68f, 34.32f, 211f),
                LandZone = new Vector3 (439.92f, 33.71f, 210.52f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35148,
                Position = new Vector3 (460.41f, 34.31f, 206.39f),
                LandZone = new Vector3 (460.12f, 33.59f, 206.25f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35149,
                Position = new Vector3 (468.54f, 34.92f, 216.16f),
                LandZone = new Vector3 (468.2f, 34.34f, 216.77f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35144,
                Position = new Vector3 (459.23f, 34.89f, 234.63f),
                LandZone = new Vector3 (459.71f, 34.33f, 234.41f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35145,
                Position = new Vector3 (454.67f, 34.75f, 254.14f),
                LandZone = new Vector3 (454.88f, 34.04f, 254.43f),
                GatheringType = 3,
                NodeSet = 14
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35146,
                Position = new Vector3 (461.14f, 35.08f, 268.28f),
                LandZone = new Vector3 (461.07f, 34.47f, 267.78f),
                GatheringType = 3,
                NodeSet = 14
            },
        } },
        // Botanist | 6 Nodes | #5 
        { new Vector2(-121, 368), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35176,
                Position = new Vector3 (-185.84f, 32.88f, 351.45f),
                LandZone = new Vector3 (-185.52f, 32.05f, 351.6f),
                GatheringType = 3,
                NodeSet = 15
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35175,
                Position = new Vector3 (-174.21f, 32.41f, 342.16f),
                LandZone = new Vector3 (-174.13f, 31.51f, 342.22f),
                GatheringType = 3,
                NodeSet = 15
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35171,
                Position = new Vector3 (-83.13f, 28.26f, 312.21f),
                LandZone = new Vector3 (-83.36f, 27.38f, 311.6f),
                GatheringType = 3,
                NodeSet = 15
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35172,
                Position = new Vector3 (-69.83f, 27.94f, 330.99f),
                LandZone = new Vector3 (-69.73f, 27.02f, 330.61f),
                GatheringType = 3,
                NodeSet = 15
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35173,
                Position = new Vector3 (-110.16f, 34.36f, 427.53f),
                LandZone = new Vector3 (-110.27f, 33.45f, 428.06f),
                GatheringType = 3,
                NodeSet = 15
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35174,
                Position = new Vector3 (-116.48f, 34.47f, 438.21f),
                LandZone = new Vector3 (-116.7f, 33.51f, 437.75f),
                GatheringType = 3,
                NodeSet = 15
            },
        } },
        // Botanist | 6 Nodes | #6
        { new Vector2(455, 243), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35177,
                Position = new Vector3 (490.19f, 36.57f, 174.08f),
                LandZone = new Vector3 (489.71f, 35.7f, 174.19f),
                GatheringType = 3,
                NodeSet = 16
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35178,
                Position = new Vector3 (441.94f, 36.48f, 176.07f),
                LandZone = new Vector3 (441.74f, 35.5f, 176.29f),
                GatheringType = 3,
                NodeSet = 16
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35180,
                Position = new Vector3 (382.5f, 35.42f, 265.39f),
                LandZone = new Vector3 (383.06f, 35.01f, 264.97f),
                GatheringType = 3,
                NodeSet = 16
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35179,
                Position = new Vector3 (395.4f, 37.31f, 276.67f),
                LandZone = new Vector3 (395.65f, 36.48f, 276.62f),
                GatheringType = 3,
                NodeSet = 16
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35181,
                Position = new Vector3 (476.83f, 40.68f, 297.1f),
                LandZone = new Vector3 (477.28f, 39.93f, 297.15f),
                GatheringType = 3,
                NodeSet = 16
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35182,
                Position = new Vector3 (510.84f, 37.4f, 283.41f),
                LandZone = new Vector3 (510.55f, 36.63f, 283.3f),
                GatheringType = 3,
                NodeSet = 16
            },
        } },
        // Botanist | 8 Nodes | #7
        { new Vector2(506, 682), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35151,
                Position = new Vector3 (559.91f, 55.79f, 672.96f),
                LandZone = new Vector3 (559.79f, 55.21f, 672.69f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35158,
                Position = new Vector3 (536.1f, 55.63f, 679.65f),
                LandZone = new Vector3 (536.37f, 54.9f, 679.87f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35154,
                Position = new Vector3 (520.14f, 56.14f, 694.22f),
                LandZone = new Vector3 (520.91f, 55.61f, 693.97f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35155,
                Position = new Vector3 (502.04f, 56.63f, 680.56f),
                LandZone = new Vector3 (502.08f, 55.68f, 680.68f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35157,
                Position = new Vector3 (481.21f, 56.12f, 660.01f),
                LandZone = new Vector3 (481.17f, 55.23f, 660.35f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35156,
                Position = new Vector3 (489.9f, 56.31f, 671.44f),
                LandZone = new Vector3 (490.2f, 55.49f, 671.69f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35152,
                Position = new Vector3 (464.35f, 55.87f, 661.57f),
                LandZone = new Vector3 (464.23f, 55.16f, 661.27f),
                GatheringType = 3,
                NodeSet = 17
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35153,
                Position = new Vector3 (452.62f, 56.02f, 663.78f),
                LandZone = new Vector3 (452.79f, 55.22f, 663.5f),
                GatheringType = 3,
                NodeSet = 17
            },
        } },
        // Botanist | x Nodes | #8
        { new Vector2(609, 478), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35183,
                Position = new Vector3 (522.22f, 53.2f, 514.22f),
                LandZone = new Vector3 (522.16f, 52.29f, 515.22f),
                GatheringType = 3,
                NodeSet = 18
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35184,
                Position = new Vector3 (529.68f, 54.04f, 488.3f),
                LandZone = new Vector3 (529.51f, 53.32f, 488.7f),
                GatheringType = 3,
                NodeSet = 18
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35185,
                Position = new Vector3 (579.7f, 54.15f, 412.27f),
                LandZone = new Vector3 (579.61f, 53.31f, 412.66f),
                GatheringType = 3,
                NodeSet = 18
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35186,
                Position = new Vector3 (592.29f, 54.46f, 429.05f),
                LandZone = new Vector3 (592.12f, 53.71f, 428.87f),
                GatheringType = 3,
                NodeSet = 18
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35187,
                Position = new Vector3 (671.4f, 64.6f, 498.43f),
                LandZone = new Vector3 (670.72f, 63.73f, 498.07f),
                GatheringType = 3,
                NodeSet = 18
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35188,
                Position = new Vector3 (651.13f, 57.95f, 522.03f),
                LandZone = new Vector3 (651.07f, 57.13f, 521.38f),
                GatheringType = 3,
                NodeSet = 18
            },
        } },
        // Botanist | x Nodes | #9
        { new Vector2(527, 630), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35194,
                Position = new Vector3 (595.76f, 58.28f, 648.99f),
                LandZone = new Vector3 (595.16f, 57.34f, 650.08f),
                GatheringType = 3,
                NodeSet = 19
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35193,
                Position = new Vector3 (602.08f, 59.59f, 659.02f),
                LandZone = new Vector3 (601.25f, 58.52f, 658.62f),
                GatheringType = 3,
                NodeSet = 19
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35192,
                Position = new Vector3 (484.43f, 56.59f, 676.53f),
                LandZone = new Vector3 (484.55f, 55.62f, 676.34f),
                GatheringType = 3,
                NodeSet = 19
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35191,
                Position = new Vector3 (463.9f, 55.91f, 671.28f),
                LandZone = new Vector3 (464.47f, 55.17f, 670.95f),
                GatheringType = 3,
                NodeSet = 19
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35190,
                Position = new Vector3 (468.77f, 55.87f, 591.9f),
                LandZone = new Vector3 (469.39f, 55.08f, 592.1f),
                GatheringType = 3,
                NodeSet = 19
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35189,
                Position = new Vector3 (492.08f, 54.21f, 574.35f),
                LandZone = new Vector3 (491.81f, 53.34f, 574.8f),
                GatheringType = 3,
                NodeSet = 19
            },
        } },
        // Botanist | x Nodes | #10
        { new Vector2(-706, 564), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35196,
                Position = new Vector3 (-684.35f, 58.86f, 503.32f),
                LandZone = new Vector3 (-684.05f, 58.24f, 503.64f),
                GatheringType = 3,
                NodeSet = 20
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35195,
                Position = new Vector3 (-701.75f, 62.75f, 512.91f),
                LandZone = new Vector3 (-701.3f, 62.17f, 513.41f),
                GatheringType = 3,
                NodeSet = 20
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35200,
                Position = new Vector3 (-772.7f, 69.54f, 593.97f),
                LandZone = new Vector3 (-773.43f, 68.96f, 593.82f),
                GatheringType = 3,
                NodeSet = 20
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35199,
                Position = new Vector3 (-770.47f, 70.37f, 608.22f),
                LandZone = new Vector3 (-769.76f, 69.6f, 608.24f),
                GatheringType = 3,
                NodeSet = 20
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35198,
                Position = new Vector3 (-651.49f, 56.65f, 620.18f),
                LandZone = new Vector3 (-651.85f, 56.27f, 619.56f),
                GatheringType = 3,
                NodeSet = 20
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35197,
                Position = new Vector3 (-649.1f, 57.32f, 604.2f),
                LandZone = new Vector3 (-649.86f, 57.03f, 604.38f),
                GatheringType = 3,
                NodeSet = 20
            },
        } },

        // Criticals
        // Botanist - Critial #1
        { new Vector2(566, -908), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 0,
                Position = new Vector3 (),
                LandZone = new Vector3 (),
                GatheringType = 2,
                NodeSet = 24
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 0,
                Position = new Vector3 (),
                LandZone = new Vector3 (),
                GatheringType = 2,
                NodeSet = 24
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 0,
                Position = new Vector3 (),
                LandZone = new Vector3 (),
                GatheringType = 2,
                NodeSet = 24
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 0,
                Position = new Vector3 (),
                LandZone = new Vector3 (),
                GatheringType = 2,
                NodeSet = 24
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 0,
                Position = new Vector3 (),
                LandZone = new Vector3 (),
                GatheringType = 2,
                NodeSet = 24
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 0,
                Position = new Vector3 (),
                LandZone = new Vector3 (),
                GatheringType = 2,
                NodeSet = 24
            },
        } },
        // Botanist - Critial #2
        { new Vector2(188, -201), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35225,
                Position = new Vector3(178.5146f, 28.33517f, -234.279f),
                LandZone = new Vector3(178.5146f, 28.33517f, -234.279f),
                GatheringType = 3,
                NodeSet = 25
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35226,
                Position = new Vector3(197.6805f, 31.65541f, -243.8829f),
                LandZone = new Vector3(197.6805f, 31.65541f, -243.8829f),
                GatheringType = 3,
                NodeSet = 25
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35230,
                Position = new Vector3(144.964f, 22.65566f, -198.4546f),
                LandZone = new Vector3(144.964f, 22.65566f, -198.4546f),
                GatheringType = 3,
                NodeSet = 25
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35229,
                Position = new Vector3(167.5034f, 24.00558f, -186.3173f),
                LandZone = new Vector3(167.5034f, 24.00558f, -186.3173f),
                GatheringType = 3,
                NodeSet = 25
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35228,
                Position = new Vector3(230.1051f, 26.79718f, -167.2313f),
                LandZone = new Vector3(230.1051f, 26.79718f, -167.2313f),
                GatheringType = 3,
                NodeSet = 25
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35227,
                Position = new Vector3(234.4044f, 27.3815f, -175.0399f),
                LandZone = new Vector3(234.4044f, 27.3815f, -175.0399f),
                GatheringType = 3,
                NodeSet = 25
            },
        } },
        // Botanist - Critical #3
        { new Vector2(748, 101), new List<GathNodeInfo>() {
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35213,
                Position = new Vector3(778.8542f, 58.51204f, 90.23239f),
                LandZone = new Vector3(778.8542f, 58.51204f, 90.23239f),
                GatheringType = 3,
                NodeSet = 26
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35214,
                Position = new Vector3(785.6716f, 59.13748f, 69.10181f),
                LandZone = new Vector3(785.6716f, 59.13748f, 69.10181f),
                GatheringType = 3,
                NodeSet = 26
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35215,
                Position = new Vector3(724.3608f, 56.97183f, 149.7798f),
                LandZone = new Vector3(724.3608f, 56.97183f, 149.7798f),
                GatheringType = 3,
                NodeSet = 26
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35216,
                Position = new Vector3(733.382f, 57.84801f, 152.2305f),
                LandZone = new Vector3(733.382f, 57.84801f, 152.2305f),
                GatheringType = 3,
                NodeSet = 26
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35217,
                Position = new Vector3(710.4752f, 53.412f, 49.43619f),
                LandZone = new Vector3(710.4752f, 53.412f, 49.43619f),
                GatheringType = 3,
                NodeSet = 26
            },
            new GathNodeInfo
            {
                ZoneId = 1237,
                NodeId = 35218,
                Position = new Vector3(700.0176f, 52.94641f, 56.65482f),
                LandZone = new Vector3(700.0176f, 52.94641f, 56.65482f),
                GatheringType = 3,
                NodeSet = 26
            },
        } },

    };
}
