using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using AOLTv1.Theme;

namespace AOLTv1.Forms
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "AOLT 정보";
            this.Size = new Size(560, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.KeyPreview = true;
            this.BackColor = DarkTheme.Background;
            this.ForeColor = DarkTheme.TextPrimary;

            // 프로그램 이름
            Label lblTitle = new Label
            {
                Text = "AOLT v1.0",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = DarkTheme.Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 20),
                Size = new Size(544, 40),
                AutoSize = false
            };
            this.Controls.Add(lblTitle);

            // 부제
            Label lblSubtitle = new Label
            {
                Text = "ANNA Object Labeling Tool",
                Font = new Font("Segoe UI", 11F),
                ForeColor = DarkTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 60),
                Size = new Size(544, 25),
                AutoSize = false
            };
            this.Controls.Add(lblSubtitle);

            // 버전 정보
            string version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0.0";
            Label lblVersion = new Label
            {
                Text = $"Version {version}",
                Font = new Font("Segoe UI", 9F),
                ForeColor = DarkTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 85),
                Size = new Size(544, 20),
                AutoSize = false
            };
            this.Controls.Add(lblVersion);

            // 구분선
            Panel separator1 = new Panel
            {
                Location = new Point(20, 115),
                Size = new Size(504, 1),
                BackColor = DarkTheme.Border
            };
            this.Controls.Add(separator1);

            // 단축키 제목
            Label lblShortcutsTitle = new Label
            {
                Text = "키보드 단축키",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = DarkTheme.TextPrimary,
                Location = new Point(20, 125),
                Size = new Size(200, 25),
                AutoSize = false
            };
            this.Controls.Add(lblShortcutsTitle);

            // 단축키 목록 ListView
            ListView lvShortcuts = new ListView
            {
                Location = new Point(20, 155),
                Size = new Size(504, 360),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BackColor = DarkTheme.Panel,
                ForeColor = DarkTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F)
            };

            lvShortcuts.Columns.Add("단축키", 160);
            lvShortcuts.Columns.Add("기능", 330);

            var shortcuts = new[]
            {
                ("Space", "재생/정지"),
                ("E", "Entry 마커"),
                ("X", "Exit 마커 (웨이포인트 생성 시 자동 관성 추적)"),
                ("Shift+T", "관성 추적 재실행 (현재 프레임~Exit)"),
                ("F1 / F2 / F3", "Person / Vehicle / Event 선택"),
                ("Ctrl+1 ~ Ctrl+0", "ID 지정 (1~10)"),
                ("Ctrl+Z", "실행취소"),
                ("Ctrl+Y", "재실행"),
                ("Delete", "선택된 박스 삭제"),
                ("A", "선택 모드"),
                ("D", "그리기 모드"),
                ("P", "속성 편집"),
                ("\u2190 / \u2192", "이전/다음 프레임 이동"),
                ("\u2191 / \u2193", "10프레임 앞/뒤 이동"),
                ("NumPad 1~3", "Occlusion 빠른 선택"),
                ("NumPad 4~6", "BodyView 빠른 선택"),
                ("NumPad 7~9", "ActionType 빠른 선택"),
            };

            foreach (var (key, desc) in shortcuts)
            {
                var item = new ListViewItem(key);
                item.SubItems.Add(desc);
                lvShortcuts.Items.Add(item);
            }

            this.Controls.Add(lvShortcuts);

            // 구분선
            Panel separator2 = new Panel
            {
                Location = new Point(20, 525),
                Size = new Size(504, 1),
                BackColor = DarkTheme.Border
            };
            this.Controls.Add(separator2);

            // 저작권
            Label lblCopyright = new Label
            {
                Text = $"\u00a9 {DateTime.Now.Year} ETRI. All rights reserved.",
                Font = new Font("Segoe UI", 8F),
                ForeColor = DarkTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(20, 535),
                Size = new Size(300, 20),
                AutoSize = false
            };
            this.Controls.Add(lblCopyright);

            // 확인 버튼
            Button btnOK = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Size = new Size(80, 30),
                Location = new Point(444, 530),
                FlatStyle = FlatStyle.Flat,
                BackColor = DarkTheme.Accent,
                ForeColor = DarkTheme.TextPrimary,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderColor = DarkTheme.Border;
            this.Controls.Add(btnOK);

            this.AcceptButton = btnOK;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape || keyData == Keys.F1)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
