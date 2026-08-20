using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

public class BleManager
{
    public event Action<string>? Log;
    public event Action<byte[]>? Frame;

    static readonly Guid Svc = Guid.Parse("00002760-08C2-11E1-9073-0E8AC72E0001");
    static readonly Guid ChN = Guid.Parse("00002760-08C2-11E1-9073-0E8AC72E0002");

    public async Task ConnectAsync(string contains)
    {
        try
        {
            Log?.Invoke("扫描蓝牙设备...");
            var devs = await DeviceInformation.FindAllAsync(
                BluetoothLEDevice.GetDeviceSelector());

            var info = devs.FirstOrDefault(d =>
                !string.IsNullOrEmpty(d.Name) &&
                d.Name.Contains(contains, StringComparison.OrdinalIgnoreCase));

            if (info == null)
            {
                Log?.Invoke($"未找到名称包含 '{contains}' 的设备");
                return;
            }

            Log?.Invoke($"发现设备: {info.Name}");
            var dev = await BluetoothLEDevice.FromIdAsync(info.Id);
            if (dev == null)
            {
                Log?.Invoke("连接失败：无法创建设备对象");
                return;
            }

            Log?.Invoke("已连接，查找服务...");
            var servicesResult = await dev.GetGattServicesAsync();
            if (servicesResult.Status != GattCommunicationStatus.Success)
            {
                Log?.Invoke($"获取服务失败: {servicesResult.Status}");
                return;
            }

            var svc = servicesResult.Services.FirstOrDefault(x => x.Uuid == Svc);
            if (svc == null)
            {
                Log?.Invoke($"未找到BMS服务，已知服务：");
                foreach (var s in servicesResult.Services)
                    Log?.Invoke($"  {s.Uuid}");
                return;
            }

            Log?.Invoke("找到BMS服务，获取特征值...");
            var charsResult = await svc.GetCharacteristicsAsync();
            if (charsResult.Status != GattCommunicationStatus.Success)
            {
                Log?.Invoke($"获取特征值失败: {charsResult.Status}");
                return;
            }

            var ch = charsResult.Characteristics.FirstOrDefault(x => x.Uuid == ChN);
            if (ch == null)
            {
                Log?.Invoke($"未找到Notify特征，已知特征：");
                foreach (var c in charsResult.Characteristics)
                    Log?.Invoke($"  {c.Uuid} (Props: {c.CharacteristicProperties})");
                return;
            }

            ch.ValueChanged += (_, e) =>
            {
                try
                {
                    // 用 DataReader 读 IBuffer —— 这是 WinRT 正确写法
                    var reader = DataReader.FromBuffer(e.CharacteristicValue);
                    byte[] d = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(d);
                    Frame?.Invoke(d);
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"数据解析异常: {ex.Message}");
                }
            };

            var notifyResult = await ch.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);
            Log?.Invoke($"订阅Notify结果: {notifyResult}");
        }
        catch (Exception ex)
        {
            Log?.Invoke($"异常: {ex.Message}");
        }
    }
}
