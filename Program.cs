using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /*
        * R e a d m e
        * -----------
        * Production equipment graphical display for LCD by Space Engineers Script.
        * 太空工程师，生产设备图形化展示脚本。
        * 
        * @see <https://github.com/se-scripts/produce-graphic>
        * @author [Hi.James](https://space.bilibili.com/368005035)
        * @author [li-guohao](https://github.com/li-guohao)
        */
        MyIni _ini = new MyIni();

        List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
        List<IMyTextPanel> panels = new List<IMyTextPanel>();
        List<IMyTextPanel> panels_Refineries = new List<IMyTextPanel>();
        List<IMyTextPanel> panels_Assemblers = new List<IMyTextPanel>();
        List<IMyAssembler> assemblers = new List<IMyAssembler>();
        List<IMyRefinery> refineries = new List<IMyRefinery>();
        List<string> spritesList = new List<string>();

        Dictionary<string, double> productionList = new Dictionary<string, double>();

        const int itemAmountInEachScreen = 35, facilityAmountInEachScreen = 20;
        const float itemBox_ColumnInterval_Float = 73, itemBox_RowInterval_Float = 102, amountBox_Height_Float = 24, facilityBox_RowInterval_Float = 25.5f;
        const string information_Section = "Information";
        int counter_ShowFacilities = 0, counter_Panel = 0;
        double counter_Logo = 0;

        Color background_Color = new Color(0, 35, 45);
        Color border_Color = new Color(0, 130, 255);

        public struct ItemList
        {
            public string Name;
            public double Amount;
        }
        ItemList[] itemList_All;
        ItemList[] itemList_Ore;
        ItemList[] itemList_Ingot;
        ItemList[] itemList_Component;
        ItemList[] itemList_AmmoMagazine;


        public struct ComparisonTable
        {
            public string Name;
            public string BluePrintName;
            public double Amount;
            public bool HasItem;
        }

        public struct Facility_Struct
        {
            public bool IsEnabled_Bool;
            public string Name;
            public bool IsProducing_Bool;
            public bool IsCooperativeMode_Bool;
            public bool IsRepeatMode_Bool;
            public string Picture;
            public double ItemAmount;
            public string InputInventory;
            public string OutputInventory;
            public string Productivity;
        }
        Facility_Struct[] refineryList;
        Facility_Struct[] assemblerList;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            SetDefultConfiguration();

            GridTerminalSystem.GetBlocksOfType(cargoContainers, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(panels, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(panels_Refineries, b => b.IsSameConstructAs(Me) && b.CustomName.Contains("LCD_Refinery_Inventory_Display:"));
            GridTerminalSystem.GetBlocksOfType(panels_Assemblers, b => b.IsSameConstructAs(Me) && b.CustomName.Contains("LCD_Assembler_Inventory_Display:"));
            GridTerminalSystem.GetBlocksOfType(assemblers, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(refineries, b => b.IsSameConstructAs(Me) && !b.BlockDefinition.ToString().Contains("Shield"));

            //  incase no screen
            if (panels.Count < 1)
            {
                if (Me.SurfaceCount > 0)
                {
                    Me.GetSurface(0).GetSprites(spritesList);
                }
            }
            else
            {
                panels[0].GetSprites(spritesList);
            }

        }

        public void DebugLCD(string text)
        {
            List<IMyTextPanel> debugPanel = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(debugPanel, b => b.IsSameConstructAs(Me) && b.CustomName == "DEBUGLCD");

            if (debugPanel.Count == 0) return;

            string temp = "";
            foreach (var panel in debugPanel)
            {
                temp = "";
                temp = panel.GetText();
            }

            foreach (var panel in debugPanel)
            {
                if (panel.ContentType != ContentType.TEXT_AND_IMAGE) panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.55f;
                panel.Font = "LoadingScreen";
                panel.WriteText(DateTime.Now.ToString(), false);
                panel.WriteText("\n", true);
                panel.WriteText(text, true);
                panel.WriteText("\n", true);
                panel.WriteText(temp, true);
            }
        }

        public void SetDefultConfiguration()
        {
            // This time we _must_ check for failure since the user may have written invalid ini.
            MyIniParseResult result;
            if (!_ini.TryParse(Me.CustomData, out result))
                throw new Exception(result.ToString());

            //  Initialize CustomData
            string dataTemp;
            dataTemp = Me.CustomData;
            if (dataTemp == "" || dataTemp == null)
            {
                _ini.Set(information_Section, "LCD_Overall_Display", "LCD_Overall_Display | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_Inventory_Display", "LCD_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_Ore_Inventory_Display", "LCD_Ore_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_Ingot_Inventory_Display", "LCD_Ingot_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_Component_Inventory_Display", "LCD_Component_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_AmmoMagazine_Inventory_Display", "LCD_AmmoMagazine_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_Refinery_Inventory_Display", "LCD_Refinery_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_Assembler_Inventory_Display", "LCD_Assembler_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                
                Me.CustomData = _ini.ToString();
            }// End if

        }

        public void GetFacilities()
        {

            refineryList = new Facility_Struct[refineries.Count];

            int k = 0;
            foreach (var refinery in refineries)
            {
                refineryList[k].Name = refinery.CustomName;
                refineryList[k].IsEnabled_Bool = refinery.Enabled;
                refineryList[k].IsProducing_Bool = refinery.IsProducing;
                refineryList[k].IsCooperativeMode_Bool = false;
                refineryList[k].IsRepeatMode_Bool = false;


                MyInventoryItem? item = refinery.InputInventory.GetItemAt(0);
                if (item == null)
                {
                    refineryList[k].Picture = "Empty";
                    refineryList[k].ItemAmount = 0;
                }
                else
                {
                    refineryList[k].Picture = item?.Type.ToString();
                    refineryList[k].ItemAmount = (double)item?.Amount;
                }

                char[] delimiterChars = { ':', '：' };
                string[] str1 = refinery.DetailedInfo.Split('%');
                string[] str2 = str1[0].Split(delimiterChars);
                refineryList[k].Productivity = str2[str2.Length - 1];

                k++;
            }

            assemblerList = new Facility_Struct[assemblers.Count];

            k = 0;
            foreach (var assembler in assemblers)
            {
                assemblerList[k].Name = assembler.CustomName;
                assemblerList[k].IsEnabled_Bool = assembler.Enabled;
                assemblerList[k].IsProducing_Bool = assembler.IsProducing;
                assemblerList[k].IsCooperativeMode_Bool = assembler.CooperativeMode;
                assemblerList[k].IsRepeatMode_Bool = assembler.Repeating;

                List<MyProductionItem> items = new List<MyProductionItem>();
                assembler.GetQueue(items);
                if (items.Count == 0)
                {
                    assemblerList[k].Picture = "Empty";
                    assemblerList[k].ItemAmount = 0;
                }
                else
                {
                    assemblerList[k].Picture = items[0].BlueprintId.ToString();
                    assemblerList[k].ItemAmount = (double)items[0].Amount;
                }

                char[] delimiterChars = { ':', '：' };
                string[] str1 = assembler.DetailedInfo.Split('%');
                string[] str2 = str1[0].Split(delimiterChars);
                assemblerList[k].Productivity = str2[str2.Length - 1];

                k++;
            }
        }

        public int FindMax(int[] arry)
        {
            int p = 0;
            for (int i = 0; i < arry.Length; i++)
            {
                if (i == 0) p = arry[i];
                else if (arry[i] > p) p = arry[i];
            }

            return p;
        }

        public void FacilitiesDivideIntoGroup(Facility_Struct[] facilityList, List<IMyTextPanel> facilityPanels)
        {
            if (facilityList.Length == 0 || facilityPanels.Count == 0) return;

            //  get all panel numbers
            int[] findMax = new int[facilityPanels.Count];
            int k = 0;
            foreach (var panel in facilityPanels)
            {
                //  get current panel number
                string[] arry = panel.CustomName.Split(':');
                findMax[k] = Convert.ToInt16(arry[1]);
                k++;
            }

            if (counter_Panel >= facilityPanels.Count) counter_Panel = 0;

            if (facilityList.Length > FindMax(findMax) * facilityAmountInEachScreen)
            {
                //  Not enough panel
                var panel = facilityPanels[counter_Panel];

                if (panel.CustomData != "0") panel.CustomData = "0";
                else panel.CustomData = "1";

                if (panel.ContentType != ContentType.SCRIPT) panel.ContentType = ContentType.SCRIPT;
                MySpriteDrawFrame frame = panel.DrawFrame();
                string[] arry = panel.CustomName.Split(':');
                if (Convert.ToInt16(arry[1]) < FindMax(findMax))
                {
                    DrawFullFacilityScreen(panel, frame, arry[1], true, facilityList);
                }
                else
                {
                    DrawFullFacilityScreen(panel, frame, arry[1], false, facilityList);
                }
                frame.Dispose();

                Echo(panel.CustomName);
            }
            else
            {
                //  Enough panel
                var panel = facilityPanels[counter_Panel];

                if (panel.CustomData != "0") panel.CustomData = "0";
                else panel.CustomData = "1";

                if (panel.ContentType != ContentType.SCRIPT) panel.ContentType = ContentType.SCRIPT;
                MySpriteDrawFrame frame = panel.DrawFrame();
                string[] arry = panel.CustomName.Split(':');
                DrawFullFacilityScreen(panel, frame, arry[1], true, facilityList);
                frame.Dispose();

                Echo(panel.CustomName);
            }

            counter_Panel++;
        }

        public string AmountUnitConversion(double amount)
        {
            double temp = 0;
            string result = "";

            if (amount >= 1000000000000000)
            {
                temp = Math.Round(amount / 1000000000000000, 1);
                result = temp.ToString() + "KT";
            }
            else if (amount >= 1000000000000)
            {
                temp = Math.Round(amount / 1000000000000, 1);
                result = temp.ToString() + "T";
            }
            else if (amount >= 1000000000)
            {
                temp = Math.Round(amount / 1000000000, 1);
                result = temp.ToString() + "G";
            }
            else if (amount >= 1000000)
            {
                temp = Math.Round(amount / 1000000, 1);
                result = temp.ToString() + "M";
            }
            else if (amount >= 1000)
            {
                temp = Math.Round(amount / 1000, 1);
                result = temp.ToString() + "K";
            }
            else
            {
                temp = Math.Round(amount, 1);
                result = temp.ToString();
            }

            return result;
        }

        public void DrawFullFacilityScreen(IMyTextPanel panel, MySpriteDrawFrame frame, string groupNumber, bool isEnoughScreen, Facility_Struct[] facilityList)
        {
            panel.WriteText("", false);

            DrawFacilityScreenFrame(panel, frame);

            for (int i = 0; i < facilityAmountInEachScreen; i++)
            {
                int k = (Convert.ToInt16(groupNumber) - 1) * facilityAmountInEachScreen + i;

                if (k > facilityList.Length - 1) return;//Last facility is finished.

                if (i == facilityAmountInEachScreen - 1)
                {
                    if (isEnoughScreen)
                    {
                        DrawSingleFacilityUnit(panel, frame, (k + 1).ToString() + ". " + facilityList[k].Name + " ×" + facilityList[k].Productivity + "%", facilityList[k].IsProducing_Bool, AmountUnitConversion(facilityList[k].ItemAmount), facilityList[k].Picture, facilityList[k].IsRepeatMode_Bool, facilityList[k].IsCooperativeMode_Bool, facilityList[k].IsEnabled_Bool, i);
                    }
                    else
                    {
                        double residus = facilityList.Length - facilityAmountInEachScreen * Convert.ToInt16(groupNumber) + 1;
                        DrawSingleFacilityUnit(panel, frame, "+ " + residus.ToString() + " Facilities", false, "0", "Empty", false, false, false, i);
                    }
                }
                else
                {
                    DrawSingleFacilityUnit(panel, frame, (k + 1).ToString() + ". " + facilityList[k].Name + " ×" + facilityList[k].Productivity + "%", facilityList[k].IsProducing_Bool, AmountUnitConversion(facilityList[k].ItemAmount), facilityList[k].Picture, facilityList[k].IsRepeatMode_Bool, facilityList[k].IsCooperativeMode_Bool, facilityList[k].IsEnabled_Bool, i);
                }

                panel.WriteText($"{(k + 1).ToString() + ".\n"}{facilityList[k].Name}", true);
                panel.WriteText("\n\n", true);
            }
        }
        public void PanelWriteText(MySpriteDrawFrame frame, string text, float x, float y, float fontSize, TextAlignment alignment)
        {
            MySprite sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = new Vector2(x, y),
                RotationOrScale = fontSize,
                Color = Color.Coral,
                Alignment = alignment,
                FontId = "LoadingScreen"
            };
            frame.Add(sprite);
        }

        public string TranslateSpriteName(string name)
        {
            string[] blueprintIds = name.Split('/');

            string temp = "Textures\\FactionLogo\\Empty.dds";
            foreach (var sprite in spritesList)
            {
                if (sprite.IndexOf(blueprintIds[blueprintIds.Length - 1]) != -1)
                {
                    temp = sprite;
                    break;
                }
            }
            return temp;
        }

        public void DrawSingleFacilityUnit(IMyTextPanel panel, MySpriteDrawFrame frame, string Name, bool isProducing, string itemAmount, string picture, bool isRepeating, bool isCooperative, bool isEnabled, int index)
        {
            //  ItemAmount box
            float h = facilityBox_RowInterval_Float;
            float width = 92f;
            float x1 = Convert.ToSingle(1 + width / 2);
            float y1 = Convert.ToSingle(1 + h / 2 + h * index) + Convert.ToSingle(panel.CustomData);
            float textY = y1 - h / 2 + 2F, textSize = 0.75f;
            //DrawBox(frame, x1, y1, width, h, background_Color);
            if (isRepeating) PanelWriteText(frame, "RE", x1 - width / 2 + 2f, textY, textSize, TextAlignment.LEFT);
            PanelWriteText(frame, itemAmount, x1 + width / 2 - 2f, textY, textSize, TextAlignment.RIGHT);

            //  picture box
            float x2 = x1 + width / 2 + h / 2 + 0.5f;
            float boxWH_Float = h - 1;
            //DrawBox(frame, x2, y1, h, h, background_Color);
            MySprite sprite;
            if (picture != "Empty")
            {
                sprite = MySprite.CreateSprite(TranslateSpriteName(picture), new Vector2(x2, y1), new Vector2(boxWH_Float, boxWH_Float));
                frame.Add(sprite);
            }

            //  isproduction box
            float x3 = x2 + h - 0.5f;
            if (isEnabled)
            {
                if (isProducing) DrawBox(frame, x3, y1, boxWH_Float, boxWH_Float, new Color(0, 140, 0));
                else DrawBox(frame, x3, y1, boxWH_Float, boxWH_Float, new Color(130, 100, 0));
            }
            else
            {
                DrawBox(frame, x3, y1, boxWH_Float, boxWH_Float, new Color(178, 9, 9));
            }
            if (isCooperative) DrawImage(frame, "Circle", x3, y1, h - 7, new Color(0, 0, 255));


            //  name box
            float nameBoxWidth = Convert.ToSingle((512 - x3 - h / 2) - 2);
            float x4 = x3 + h / 2 + nameBoxWidth / 2 + 0.5f;
            //DrawBox(frame, x4, y1, nameBoxWidth, h, background_Color);
            PanelWriteText(frame, Name, x4 - nameBoxWidth / 2 + 1f, textY, textSize, TextAlignment.LEFT);
        }
        public void DrawImage(MySpriteDrawFrame frame, string name, float x, float y, float width, Color co)
        {
            MySprite sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = name,
                Position = new Vector2(x, y),
                Size = new Vector2(width - 6, width - 6),
                Color = co,
                Alignment = TextAlignment.CENTER,
            };
            frame.Add(sprite);
        }

        public void DrawBox(MySpriteDrawFrame frame, float x, float y, float width, float height, Color background_Color)
        {
            MySprite sprite;
            sprite = MySprite.CreateSprite("SquareSimple", new Vector2(x, y), new Vector2(width - 2, height - 2));
            sprite.Color = background_Color;
            frame.Add(sprite);
        }

        public void DrawFacilityScreenFrame(IMyTextPanel panel, MySpriteDrawFrame frame)
        {
            float lineWith_FLoat = 1f;

            //DrawBox(frame, 512 / 2, 512 / 2 + Convert.ToSingle(panel.CustomData), 514, 514, Color.Black);
            DrawBox(frame, 512 / 2, 512 / 2 + Convert.ToSingle(panel.CustomData), 514, 514, background_Color);

            for (int i = 0; i <= 20; i++)
            {
                DrawBox(frame, 512 / 2, 1 + facilityBox_RowInterval_Float * i, 512, lineWith_FLoat, Color.Black);
            }

            float x1 = 1, x2 = x1 + 92, x3 = x2 + facilityBox_RowInterval_Float, x4 = x3 + facilityBox_RowInterval_Float, x5 = 512, x31 = (x3 + x4) / 2;
            DrawBox(frame, x1, 512 / 2, lineWith_FLoat, 512, Color.Black);
            DrawBox(frame, x2, 512 / 2, lineWith_FLoat, 512, Color.Black);
            DrawBox(frame, x31, 512 / 2, facilityBox_RowInterval_Float + 2, 512, Color.Black);
            DrawBox(frame, x5, 512 / 2, lineWith_FLoat, 512, Color.Black);

        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"{DateTime.Now}");
            Echo("Program is running.");

            DateTime beforDT = System.DateTime.Now;
            if (counter_ShowFacilities >= panels_Refineries.Count + panels_Assemblers.Count + 2) counter_ShowFacilities = 0;

            GetFacilities();

            if (counter_ShowFacilities <= panels_Refineries.Count)
            {
                if (refineries.Count > 0) FacilitiesDivideIntoGroup(refineryList, panels_Refineries);
            }
            else
            {
                if (assemblers.Count > 0) FacilitiesDivideIntoGroup(assemblerList, panels_Assemblers);
            }

            counter_ShowFacilities++;

            DateTime afterDT = System.DateTime.Now;
            TimeSpan ts = afterDT.Subtract(beforDT);
            Echo("Total cost ms：" + ts.TotalMilliseconds);

            DebugLCD("Total cost ms: " + ts.TotalMilliseconds);

        }
    }
}
