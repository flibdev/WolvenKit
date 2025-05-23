using static WolvenKit.RED4.Types.Enums;

namespace WolvenKit.RED4.Types
{
	public partial class DataTermDeviceBlackboardDef : DeviceBaseBlackboardDef
	{
		[Ordinal(7)] 
		[RED("fastTravelPoint")] 
		public gamebbScriptID_Variant FastTravelPoint
		{
			get => GetPropertyValue<gamebbScriptID_Variant>();
			set => SetPropertyValue<gamebbScriptID_Variant>(value);
		}

		[Ordinal(8)] 
		[RED("triggerWorldMap")] 
		public gamebbScriptID_Bool TriggerWorldMap
		{
			get => GetPropertyValue<gamebbScriptID_Bool>();
			set => SetPropertyValue<gamebbScriptID_Bool>(value);
		}

		[Ordinal(9)] 
		[RED("passengerCount")] 
		public gamebbScriptID_Int32 PassengerCount
		{
			get => GetPropertyValue<gamebbScriptID_Int32>();
			set => SetPropertyValue<gamebbScriptID_Int32>(value);
		}

		[Ordinal(10)] 
		[RED("subwayGateOpen")] 
		public gamebbScriptID_Bool SubwayGateOpen
		{
			get => GetPropertyValue<gamebbScriptID_Bool>();
			set => SetPropertyValue<gamebbScriptID_Bool>(value);
		}

		public DataTermDeviceBlackboardDef()
		{
			FastTravelPoint = new gamebbScriptID_Variant();
			TriggerWorldMap = new gamebbScriptID_Bool();
			PassengerCount = new gamebbScriptID_Int32();
			SubwayGateOpen = new gamebbScriptID_Bool();

			PostConstruct();
		}

		partial void PostConstruct();
	}
}
