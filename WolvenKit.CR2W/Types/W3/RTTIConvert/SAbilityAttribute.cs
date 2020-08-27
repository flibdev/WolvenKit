using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
	[DataContract(Namespace = "")]
	[REDMeta]
	public class SAbilityAttribute : CVariable
	{
		[RED("name")] 		public CName Name { get; set;}

		[RED("type")] 		public CEnum<EAbilityAttributeType> Type { get; set;}

		[RED("alwaysRandom")] 		public CBool AlwaysRandom { get; set;}

		[RED("min")] 		public CFloat Min { get; set;}

		[RED("max")] 		public CFloat Max { get; set;}

		[RED("precision")] 		public CInt8 Precision { get; set;}

		[RED("displayPerc")] 		public CBool DisplayPerc { get; set;}

		public SAbilityAttribute(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name){ }

		public static new CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new SAbilityAttribute(cr2w, parent, name);

		public override void Read(BinaryReader file, uint size) => base.Read(file, size);

		public override void Write(BinaryWriter file) => base.Write(file);

	}
}