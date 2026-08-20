using System;
using System.Windows.Forms;
using System.Text;

var parser = new BmsParser();
var ble = new BleManager();
var box = new TextBox { Dock = DockStyle.Fill, Multiline = true, Font = new System.Drawing.Font("Consolas", 10) };
var btn = new Button { Text = "连接锂安BMS", Dock = DockStyle.Top };

ble.Log += m => box.Invoke(() => box.AppendText(m + "\r\n"));
ble.Frame += f =>
{
    if (parser.Parse(f))
    {
        var sb = new StringBuilder();
        sb.AppendLine($"电压:{parser.Voltage:F2} V");
        sb.AppendLine($"电流:{parser.Current:F2} A");
        sb.AppendLine($"SOC :{parser.Soc} %");
        sb.AppendLine($"温度:{parser.Temperature:F1} ℃");
        sb.AppendLine($"循环:{parser.Cycles}");
        for (int i = 0; i < parser.Cells.Count; i++)
            sb.AppendLine($"  电芯{i + 1}:{parser.Cells[i]:F3} V");
        box.Invoke(() => box.Text = sb.ToString());
    }
};

btn.Click += async (_, _) => { btn.Enabled = false; await ble.ConnectAsync("BMS"); };

var f = new Form { Text = "锂安BMS Windows监控", Width = 480, Height = 640 };
f.Controls.Add(box); f.Controls.Add(btn);
Application.Run(f);
