/*
	Grafiti library

    Copyright 2008  Alessandro De Nardi <alessandro.denardi@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License as
    published by the Free Software Foundation; either version 3 of 
    the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Xml;
using System.Globalization;

namespace Grafiti
{
    public static class Settings
    {
        private const string SETTINGS_FILENAME = "settings.xml";

        // Attributes' names, values and default values:


        // Camera resolution ratio (width resolution over height resolution).
        // The unit, in all the transmitted coordinates data, will correspond to the height of the screen.
        private const string CAM_RESO_RATIO_NAME = "CAMERA_RESOLUTION_RATIO";
        internal static readonly float CAM_RESO_RATIO;
        internal const float CAM_RESO_RATIO_DEFAULT = (float)(4d / 3d);

        // Offset for Tuio's x coordinates can be 0 for rectangular touch table or
        // (1f - 1f / CAMERA_RESOLUTION_RATIO) / 2 for round or square tables (ratio 1:1),
        // to adjust the origin such the origin corresponds to the upper left corner of the table.
        private const string RECTANGULAR_TABLE_NAME = "RECTANGULAR_TABLE";
        internal static readonly bool RECTANGULAR_TABLE;
        internal const bool RECTANGULAR_TABLE_DEFAULT = false;


        // The maximum time in milliseconds between cursors to determine the group's INITIAL and FINAL lists
        private const string GROUPING_SYNCH_TIME_NAME = "GROUPING_SYNCH_TIME";
        internal static readonly int GROUPING_SYNCH_TIME;
        internal const int GROUPING_SYNCH_TIME_DEFAULT = 200;//2000;


        // Maximum space between traces to be grouped together
        private const string GROUPING_SPACE_NAME = "GROUPING_SPACE";
        internal static readonly float GROUPING_SPACE;
        internal const float GROUPING_SPACE_DEFAULT = 0.2f;


        // If set to true, the clustering will consider the distance to the closest living (not removed) trace
        // If set to false it will consider also resurrectable (recently removed) traces. This latter
        // case will support non continuous gestures like an 'x' produced with one finger that will
        // be temporarly removed from the surface between the drawing of the two crossing lines.
        private const string CLUSTERING_ONLY_WITH_ALIVE_TRACES_NAME = "CLUSTERING_ONLY_WITH_ALIVE_TRACES";
        internal static readonly bool CLUSTERING_ONLY_WITH_ALIVE_TRACES;
        internal const bool CLUSTERING_ONLY_WITH_ALIVE_TRACES_DEFAULT = true;


        // Maximum time in millisecond between a 'remove' of a cursor and an 'add' of another cursor, to
        // associate the cursors to the same (discontinuous) trace.
        private const string TRACE_TIME_GAP_NAME = "TRACE_TIME_GAP";
        internal static readonly int TRACE_TIME_GAP;
        internal const int TRACE_TIME_GAP_DEFAULT = 200; //2000;


        // Maximum space between a 'remove' of a cursor and an 'add' of another cursor, to
        // associate the cursors to the same (discontinuous) trace.
        private const string TRACE_SPACE_GAP_NAME = "TRACE_SPACE_GAP";
        internal static readonly float TRACE_SPACE_GAP;
        internal const float TRACE_SPACE_GAP_DEFAULT = 0.02f;


        // Group's target lists used to determine which LGRs will be called
        private const string LGR_TARGET_LIST_NAME = "LGR_TARGET_LIST";
        internal static readonly LGRTargetLists LGR_TARGET_LIST;
        internal const LGRTargetLists LGR_TARGET_LIST_DEFAULT = LGRTargetLists.INTERSECTION_TARGET_LIST;
        internal enum LGRTargetLists
        {
            INITIAL_TARGET_LIST = 0,
            INTERSECTION_TARGET_LIST = 1,
            //FINAL_TARGET_LIST = 2
        }


        // Precedence to GGRs over LGRs in case of same priority number
        private const string PRECEDENCE_GGRS_OVER_LGRS_NAME = "PRECEDENCE_GGRS_OVER_LGRS";
        internal static readonly bool PRECEDENCE_GGRS_OVER_LGRS;
        internal const bool PRECEDENCE_GGRS_OVER_LGRS_DEFAULT = true;


        // Behaviour of exclusive GRs. If this flag is set to true then a winning exclusive GR, when
        // it's armed it will block the other unarmed GRs *and* the currently interpreting which
        // priority number is non-negative. If the flag is set to false, as by default, a winning exclusive
        // GR, when it's armed it will block only the other unarmed GRs.
        private const string EXCLUSIVE_BLOCK_INTERPRETING_NAME = "EXCLUSIVE_BLOCK_INTERPRETING";
        internal static readonly bool EXCLUSIVE_BLOCK_INTERPRETING;
        internal const bool EXCLUSIVE_BLOCK_INTERPRETING_DEFAULT = false;


        public static float CameraResolutionRatio { get { return CAM_RESO_RATIO; } }
        public static bool GetRectangularTable { get { return RECTANGULAR_TABLE; } }
        public static int GroupingSynchTime { get { return GROUPING_SYNCH_TIME; } }
        public static float GroupingSpace { get { return GROUPING_SPACE; } }
        public static bool ClusteringOnlyWithAliveTraces { get { return CLUSTERING_ONLY_WITH_ALIVE_TRACES; } }
        public static int TraceTimeGap { get { return TRACE_TIME_GAP; } }
        public static float TraceSpaceGap { get { return TRACE_SPACE_GAP; } }
        public static int LGRTargetList { get { return (int)LGR_TARGET_LIST; } }
        public static bool PrecedeceGGRsOverLGRs { get { return PRECEDENCE_GGRS_OVER_LGRS; } }
        public static bool ExclusiveBlockInterpreting { get { return EXCLUSIVE_BLOCK_INTERPRETING; } }        


        static Settings()
        {
            // Default settings
            CAM_RESO_RATIO = CAM_RESO_RATIO_DEFAULT;
            RECTANGULAR_TABLE = RECTANGULAR_TABLE_DEFAULT;
            GROUPING_SYNCH_TIME = GROUPING_SYNCH_TIME_DEFAULT;
            GROUPING_SPACE = GROUPING_SPACE_DEFAULT;
            CLUSTERING_ONLY_WITH_ALIVE_TRACES = CLUSTERING_ONLY_WITH_ALIVE_TRACES_DEFAULT;
            TRACE_TIME_GAP = TRACE_TIME_GAP_DEFAULT;
            TRACE_SPACE_GAP = TRACE_SPACE_GAP_DEFAULT;
            LGR_TARGET_LIST = LGR_TARGET_LIST_DEFAULT;
            PRECEDENCE_GGRS_OVER_LGRS = PRECEDENCE_GGRS_OVER_LGRS_DEFAULT;
            EXCLUSIVE_BLOCK_INTERPRETING = EXCLUSIVE_BLOCK_INTERPRETING_DEFAULT;


            // Get settings.xml file path
            string fullAppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
            string fullAppPath = System.IO.Path.GetDirectoryName(fullAppName);
            string settingsFullFileName;
            if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                settingsFullFileName = System.IO.Path.Combine(fullAppPath, SETTINGS_FILENAME).Substring(5);
            else
                settingsFullFileName = System.IO.Path.Combine(fullAppPath, SETTINGS_FILENAME).Substring(6);
            if (!System.IO.File.Exists(settingsFullFileName))
            {
                Console.WriteLine("File '{0}' not found. Using default settings.", settingsFullFileName);
                return;
            }

            // Set up xml reader
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreWhitespace = true;
            readerSettings.IgnoreComments = true;
            XmlReader reader = null;
            reader = XmlReader.Create(settingsFullFileName, readerSettings);

            // Read <settings> tag
            reader.Read();
            if (!(reader.Name == "settings"))
            {
                Console.WriteLine("Coudn't find tag <settings> in file '{0}'. Using default settings.", settingsFullFileName);
                return;
            }

            // Parse attributes and initialize values

            if (reader.MoveToAttribute(CAM_RESO_RATIO_NAME))
                try
                {
                    CAM_RESO_RATIO = (float)System.Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                }
                catch (Exception e)
                {
                    PrintParsingError(settingsFullFileName, reader, e);
                }

            if (reader.MoveToAttribute(RECTANGULAR_TABLE_NAME))
                try
                {
                    RECTANGULAR_TABLE = Boolean.Parse(reader.Value);
                }
                catch (Exception e)
                {
                    PrintParsingError(settingsFullFileName, reader, e);
                }

            if (reader.MoveToAttribute(GROUPING_SYNCH_TIME_NAME))
                try
                {
                    GROUPING_SYNCH_TIME = int.Parse(reader.Value);
                }
                catch (Exception e)
                {
                    PrintParsingError(settingsFullFileName, reader, e);
                }

            if (reader.MoveToAttribute(GROUPING_SPACE_NAME))
                try
                {
                    GROUPING_SPACE = (float)System.Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                }
                catch (Exception e)
                {
                    PrintParsingError(settingsFullFileName, reader, e);
                }

            if (reader.MoveToAttribute(CLUSTERING_ONLY_WITH_ALIVE_TRACES_NAME))
                try
                {
                    CLUSTERING_ONLY_WITH_ALIVE_TRACES = Boolean.Parse(reader.Value);
                }
                catch (Exception e)
                {
                    PrintParsingError(settingsFullFileName, reader, e);
                }

            if (reader.MoveToAttribute(TRACE_TIME_GAP_NAME))
                try
                {
                    TRACE_TIME_GAP = int.Parse(reader.Value);
                }
                catch (Exception e)
                {
                    PrintParsingError(settingsFullFileName, reader, e);
                }

            if (reader.MoveToAttribute(TRACE_SPACE_GAP_NAME))
                try
                {
                    TRACE_SPACE_GAP = (float)System.Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                }
                catch (Exception e)
                {
                    PrintParsingError(settingsFullFileName, reader, e);
                }

            if (reader.MoveToAttribute(LGR_TARGET_LIST_NAME))
                try
                {
                    switch (int.Parse(reader.Value))
                    {
                        case (int)LGRTargetLists.INITIAL_TARGET_LIST:
                            LGR_TARGET_LIST = LGRTargetLists.INITIAL_TARGET_LIST;
                            break;
                        case (int)LGRTargetLists.INTERSECTION_TARGET_LIST:
                            LGR_TARGET_LIST = LGRTargetLists.INTERSECTION_TARGET_LIST;
                            break;
                        default:
                            throw new FormatException("Invalid value.");
                    }
                }
                catch (Exception e)
                {
                    PrintParsingError(settingsFullFileName, reader, e);
                }

            if (reader.MoveToAttribute(PRECEDENCE_GGRS_OVER_LGRS_NAME))
                try
                {
                    PRECEDENCE_GGRS_OVER_LGRS = Boolean.Parse(reader.Value);
                }
                catch (Exception e)
                {
                    PrintParsingError(settingsFullFileName, reader, e);
                }

            if (reader.MoveToAttribute(EXCLUSIVE_BLOCK_INTERPRETING_NAME))
                try
                {
                    EXCLUSIVE_BLOCK_INTERPRETING = Boolean.Parse(reader.Value);
                }
                catch (Exception e)
                {
                    PrintParsingError(settingsFullFileName, reader, e);
                }
        }

        private static void PrintParsingError(string settingsFullFileName, XmlReader reader, Exception e)
        {
            Console.WriteLine("{0}\nError occurred while parsing attribute '{1}'.\nPlease check file '{2}'.\nThe value will be set by default.",
                e.Message, reader.Name, settingsFullFileName);
        }

        public static void Initialize() { }
    }
}
