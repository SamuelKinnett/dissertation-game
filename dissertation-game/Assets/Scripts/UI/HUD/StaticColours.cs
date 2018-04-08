using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// This class makes managing team colours much easier, since they can be
    /// changed in one place rather than having to refactor everywhere that
    /// uses them.
    /// </summary>
    public static class StaticColours
    {
        public static Color RedTeamColour = new Color(255, 0, 0);
        public static Color BlueTeamColour = new Color(0, 0, 255);
        public static Color NeautralColour = new Color(128, 128, 128);
    }
}
