using System;
using System.Collections.Generic;
using System.Text;

namespace TVTComment.ViewModels
{
    class ViewSettings
    {
        public bool WindowTopmost { get; set; } = false;
        public double WindowOpacity { get; set; } = 1.0;
        public Model.Serialization.WindowPositionEntity MainWindowPosition { get; set; } = new Model.Serialization.WindowPositionEntity();
        public Model.Serialization.WindowPositionEntity NgSettingWindowPosition { get; set; } = new Model.Serialization.WindowPositionEntity();
        public ListViewColumnViewModel[] ChatListViewColumns { get; set; } = null;
        public double WindowFontSize { get; set; } = 12.0;
    }
}
