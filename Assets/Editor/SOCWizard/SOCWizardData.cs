using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOCWizard.Data
{
    public class SOCWizardData
    {
        public bool _bakeEditorBuildList;
        public bool _spawnOnAsset;
        public bool _isPortalOpen;
        public bool _showBakeTools;
        public bool _showSceneTool;
        public bool _showTest;
        public bool _showUmbraInfo;
        public bool _showSocInfo;
        public bool _showVisOptions;
        public float DefBackfaceThreshold = 100;
        public float DefSmallestHole = 0.25f;
        public float DefSmallestOccluder = 5;
    }
}
