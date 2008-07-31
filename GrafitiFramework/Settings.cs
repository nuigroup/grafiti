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

        // Touch surface size ratio (width / height).
        // The unit, in all the transmitted coordinates data, will correspond to the height of the screen.
        private const string SCREEN_RATIO_NAME = "SCREEN_RATIO";
        internal static readonly float SCREEN_RATIO;
        internal const float SCREEN_RATIO_DEFAULT = 1.333333f; // 4/3

        // Group's targeting method
        private const string INTERSECTION_MODE_NAME = "INTERSECTION_MODE";
        internal static readonly bool INTERSECTION_MODE;
        internal const bool INTERSECTION_MODE_DEFAULT = true;

        // The maximum time in milliseconds between cursors to determine the group's INITIAL and FINAL lists
        private const string GROUPING_SYNCH_TIME_NAME = "GROUPING_SYNCH_TIME";
        internal static readonly int GROUPING_SYNCH_TIME;
        internal const int GROUPING_SYNCH_TIME_DEFAULT = 200;//2000;

        // Maximum space between traces to be grouped together
        private const string GROUPING_SPACE_NAME = "GROUPING_SPACE";
        internal static readonly float GROUPING_SPACE;
        internal const float GROUPING_SPACE_DEFAULT = 0.2f;

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
        internal enum LGRTargetLists
        {
            INITIAL_TARGET_LIST = 0,
            INTERSECTION_TARGET_LIST = 1,
            //FINAL_TARGET_LIST = 2
        }
        private const string LGR_TARGET_LIST_NAME = "LGR_TARGET_LIST";
        internal static readonly LGRTargetLists LGR_TARGET_LIST;
        internal const LGRTargetLists LGR_TARGET_LIST_DEFAULT = LGRTargetLists.INTERSECTION_TARGET_LIST;

        static Settings()
        {
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
                Console.WriteLine("File '{0}' doesn't exist. The path can't contain spaces or characters like '#'.", settingsFullFileName);
                goto error;
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
                Console.WriteLine("Coudn't find tag <settings> in file '{0}'.", settingsFullFileName);
                goto error;
            }

            // Parse attributes and initialize values
            string currentAttribute = "";
            try
            {
                currentAttribute = SCREEN_RATIO_NAME;
                reader.MoveToAttribute(currentAttribute);
                SCREEN_RATIO = (float)System.Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture.NumberFormat);

                currentAttribute = INTERSECTION_MODE_NAME;
                reader.MoveToAttribute(currentAttribute);
                INTERSECTION_MODE = Boolean.Parse(reader.Value);

                currentAttribute = GROUPING_SYNCH_TIME_NAME;
                reader.MoveToAttribute(currentAttribute);
                GROUPING_SYNCH_TIME = int.Parse(reader.Value);

                currentAttribute = GROUPING_SPACE_NAME;
                reader.MoveToAttribute(currentAttribute);
                GROUPING_SPACE = (float)System.Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture.NumberFormat);

                currentAttribute = TRACE_TIME_GAP_NAME;
                reader.MoveToAttribute(currentAttribute);
                TRACE_TIME_GAP = int.Parse(reader.Value);

                currentAttribute = TRACE_SPACE_GAP_NAME;
                reader.MoveToAttribute(currentAttribute);
                TRACE_SPACE_GAP = (float)System.Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture.NumberFormat);

                currentAttribute = LGR_TARGET_LIST_NAME;
                reader.MoveToAttribute(currentAttribute);
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
                Console.WriteLine("{0}\nThe error occurred while checking attribute '{1}'.\nPlease check file '{2}'.", e.Message, currentAttribute, settingsFullFileName);
                goto error;
            }

            return;


            // If some error occured use the default settings. I like goto, so?
        error:
            Console.WriteLine("Using default settings.");
            SCREEN_RATIO = SCREEN_RATIO_DEFAULT;
            INTERSECTION_MODE = INTERSECTION_MODE_DEFAULT;
            GROUPING_SYNCH_TIME = GROUPING_SYNCH_TIME_DEFAULT;
            GROUPING_SPACE = GROUPING_SPACE_DEFAULT;
            TRACE_TIME_GAP = TRACE_TIME_GAP_DEFAULT;
            TRACE_SPACE_GAP = TRACE_SPACE_GAP_DEFAULT;
            LGR_TARGET_LIST = LGR_TARGET_LIST_DEFAULT;

        }

        public static void Initialize() { }

        public static float GetScreenRatio()
        {
            return SCREEN_RATIO;
        }
        public static bool GetIntersectionMode()
        {
            return INTERSECTION_MODE;
        }
        public static int GetGroupingSynchTime()
        {
            return GROUPING_SYNCH_TIME;
        }
        public static float GetGroupingSpace()
        {
            return GROUPING_SPACE;
        }
        public static int GetTraceTimeGap()
        {
            return TRACE_TIME_GAP;
        }
        public static float GetTraceSpaceGap()
        {
            return TRACE_SPACE_GAP;
        }
        public static int GetLGRTargetList()
        {
            return (int)LGR_TARGET_LIST;
        }

    }
}