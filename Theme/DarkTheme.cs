namespace ASLTv1.Theme
{
    public static class DarkTheme
    {
        // 기본 배경색
        public static readonly Color Background = Color.FromArgb(0x1E, 0x1E, 0x1E);
        public static readonly Color Panel = Color.FromArgb(0x25, 0x25, 0x26);
        public static readonly Color Header = Color.FromArgb(0x2D, 0x2D, 0x30);

        // 강조색
        public static readonly Color Accent = Color.FromArgb(0x00, 0x78, 0xD4);
        public static readonly Color AccentLight = Color.FromArgb(0x1A, 0x8C, 0xE0);

        // 텍스트
        public static readonly Color TextPrimary = Color.FromArgb(0xD4, 0xD4, 0xD4);
        public static readonly Color TextSecondary = Color.FromArgb(0x9D, 0x9D, 0x9D);

        // 버튼
        public static readonly Color ButtonBg = Color.FromArgb(0x3E, 0x3E, 0x42);
        public static readonly Color ButtonHover = Color.FromArgb(0x50, 0x50, 0x57);

        // 테두리
        public static readonly Color Border = Color.FromArgb(0x3F, 0x3F, 0x46);

        // 상태 색상
        public static readonly Color SuccessGreen = Color.FromArgb(0x4E, 0xC9, 0xB0);
        public static readonly Color DangerRed = Color.FromArgb(0xC7, 0x36, 0x36);
        public static readonly Color WarningOrange = Color.FromArgb(0xCE, 0x91, 0x78);

        // 레이블 색상 (바운딩박스용)
        public static readonly Color PersonColor = Color.FromArgb(255, 107, 107);
        public static readonly Color VehicleColor = Color.FromArgb(107, 158, 255);
        public static readonly Color EventColor = Color.FromArgb(107, 255, 107);

        // Person/Vehicle/Event 연한 배경
        public static readonly Color PersonBgLight = Color.FromArgb(60, 30, 30);
        public static readonly Color VehicleBgLight = Color.FromArgb(30, 30, 60);
        public static readonly Color EventBgLight = Color.FromArgb(30, 60, 30);

        /// <summary>
        /// Form 전체에 다크 테마를 적용합니다.
        /// </summary>
        public static void Apply(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = TextPrimary;
            ApplyToControls(form.Controls);
        }

        private static void ApplyToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case Button btn:
                        ApplyButton(btn);
                        break;
                    case TextBox tb:
                        tb.BackColor = Panel;
                        tb.ForeColor = TextPrimary;
                        tb.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case ComboBox cb:
                        ApplyComboBox(cb);
                        break;
                    case ListView lv:
                        ApplyListView(lv);
                        break;
                    case GroupBox gb:
                        ApplyGroupBox(gb);
                        break;
                }

                if (control.HasChildren)
                {
                    ApplyToControls(control.Controls);
                }
            }
        }

        public static void ApplyButton(Button btn, bool isAccent = false)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Border;
            btn.FlatAppearance.BorderSize = 1;
            btn.ForeColor = TextPrimary;
            btn.BackColor = isAccent ? Accent : ButtonBg;
            btn.Cursor = Cursors.Hand;

            btn.MouseEnter += (s, e) =>
            {
                if (btn.BackColor == ButtonBg)
                    btn.BackColor = ButtonHover;
            };
            btn.MouseLeave += (s, e) =>
            {
                if (btn.BackColor == ButtonHover)
                    btn.BackColor = ButtonBg;
            };
        }

        public static void ApplyPanel(System.Windows.Forms.Panel panel)
        {
            panel.BackColor = Panel;
        }

        public static void ApplyListView(ListView lv)
        {
            lv.BackColor = Panel;
            lv.ForeColor = TextPrimary;
            lv.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void ApplyGroupBox(GroupBox gb)
        {
            gb.BackColor = Background;
        }

        public static void ApplyComboBox(ComboBox cb)
        {
            cb.BackColor = Panel;
            cb.ForeColor = TextPrimary;
            cb.FlatStyle = FlatStyle.Flat;
        }
    }
}
