using System;
using System.IO;
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
        var devs = await DeviceInformation.FindAllAsync(
            BluetoothLEDevice.GetDeviceSelector());
        var info = devs.FirstOrDefault(d =>
            !string.IsNullOrEmpty(d.Name) && d.Name.Contains(contains, StringComparison.OrdinalIgnoreCase));
        if (info == null) { Log?.Invoke("未找到设备"); return; }

        var dev = await BluetoothLEDevice.FromIdAsync(info.Id);
        Log?.Invoke($"连上 {dev.Name}");
        var svc = (await dev.GetGattServicesAsync()).Services
            .First(x => x.Uuid == Svc);
        var ch = (await svc.GetCharacteristicsAsync()).Characteristics
            .First(x => x.Uuid == ChN);

        ch.ValueChanged += (_, e) =>
        {
            var buf = e.CharacteristicValue;
            byte[] d = new byte[buf.Length];
            using var rd = buf.AsStream();
            rd.Read(d, 0, d.Length);
            Frame?.Invoke(d);
        };
        await ch.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify);
        Log?.Invoke("已订阅 Notify");
    }
}
