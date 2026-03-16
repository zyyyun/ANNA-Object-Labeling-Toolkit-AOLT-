using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AOLTv1.Theme;

namespace AOLTv1.Forms
{
    // 드래그 가능한 Person 속성 창
    public class PersonAttributeWindow : Form
    {
        private int personId;
        private int frameIndex;
        private System.Drawing.Point dragOffset;
        private bool isDragging = false;
        private Label lblContent;
        private Button btnClose;

        public int PersonId => personId;
        public int FrameIndex => frameIndex;

        public PersonAttributeWindow(int personId, int frameIndex, Dictionary<string, object> attributes, System.Drawing.Point initialLocation)
        {
            this.personId = personId;
            this.frameIndex = frameIndex;

            // Form 설정
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = DarkTheme.Background;
            this.Size = new System.Drawing.Size(250, 200);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = initialLocation;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Opacity = 0.95;

            // 패널 (테두리 효과)
            Panel borderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                BackColor = DarkTheme.Accent // 파란색 테두리
            };
            this.Controls.Add(borderPanel);

            // 내부 패널
            Panel innerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.Background
            };
            borderPanel.Controls.Add(innerPanel);

            // 제목 바
            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = DarkTheme.Header,
                Cursor = Cursors.SizeAll
            };
            innerPanel.Controls.Add(titleBar);

            // 제목 레이블
            Label lblTitle = new Label
            {
                Text = $"Person {personId:D2}",
                ForeColor = DarkTheme.TextPrimary,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new System.Drawing.Point(10, 5),
                AutoSize = true
            };
            titleBar.Controls.Add(lblTitle);

            // 닫기 버튼
            btnClose = new Button
            {
                Text = "\u2715",
                Size = new System.Drawing.Size(25, 25),
                Location = new System.Drawing.Point(220, 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = DarkTheme.Header,
                ForeColor = DarkTheme.TextPrimary,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            titleBar.Controls.Add(btnClose);

            // 드래그 이벤트
            titleBar.MouseDown += TitleBar_MouseDown;
            titleBar.MouseMove += TitleBar_MouseMove;
            titleBar.MouseUp += TitleBar_MouseUp;

            // 내용 레이블
            lblContent = new Label
            {
                Location = new System.Drawing.Point(10, 40),
                Size = new System.Drawing.Size(230, 150),
                ForeColor = DarkTheme.TextPrimary,
                Font = new Font("Consolas", 9F),
                AutoSize = false
            };
            innerPanel.Controls.Add(lblContent);

            // 속성 텍스트 설정
            UpdateAttributes(attributes);
        }

        public void UpdateAttributes(Dictionary<string, object> attributes)
        {
            if (attributes == null || attributes.Count == 0)
            {
                lblContent.Text = "(속성 없음)";
                return;
            }

            var lines = new List<string>();
            foreach (var kvp in attributes)
            {
                string value = FormatAttributeValue(kvp.Value);
                lines.Add($"{kvp.Key}: {value}");
            }

            lblContent.Text = string.Join("\r\n", lines);
        }

        private string FormatAttributeValue(object value)
        {
            if (value == null)
                return "-";

            // 배열/리스트인 경우 처리
            if (value is List<string> listValue)
            {
                if (listValue.Count == 0)
                    return "-";
                // 한국어로 변환하여 표시
                var koreanValues = listValue.Select(v => PersonAttributesForm.GetAttributeValueKorean(v)).ToList();
                return string.Join(", ", koreanValues);
            }
            else if (value is string[] arrayValue)
            {
                if (arrayValue.Length == 0)
                    return "-";
                var koreanValues = arrayValue.Select(v => PersonAttributesForm.GetAttributeValueKorean(v)).ToList();
                return string.Join(", ", koreanValues);
            }
            else if (value is string stringValue)
            {
                // 단일 값인 경우 한국어로 변환
                return PersonAttributesForm.GetAttributeValueKorean(stringValue);
            }
            else
            {
                // 기타 타입은 문자열로 변환 후 한국어 변환 시도
                string strValue = value.ToString();
                return PersonAttributesForm.GetAttributeValueKorean(strValue);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragOffset = new System.Drawing.Point(e.X, e.Y);
            }
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                System.Drawing.Point currentScreenPos = PointToScreen(e.Location);
                this.Location = new System.Drawing.Point(
                    currentScreenPos.X - dragOffset.X,
                    currentScreenPos.Y - dragOffset.Y);
            }
        }

        private void TitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }
    }
}
