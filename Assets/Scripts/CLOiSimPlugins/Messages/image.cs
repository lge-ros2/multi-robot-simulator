// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: image.proto
// </auto-generated>

#region Designer generated code
#pragma warning disable CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
namespace cloisim.msgs
{

    [global::ProtoBuf.ProtoContract()]
    public partial class Image : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"width", IsRequired = true)]
        public uint Width { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"height", IsRequired = true)]
        public uint Height { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"pixel_format", IsRequired = true)]
        public uint PixelFormat { get; set; }

        [global::ProtoBuf.ProtoMember(4, Name = @"step", IsRequired = true)]
        public uint Step { get; set; }

        [global::ProtoBuf.ProtoMember(5, Name = @"data", IsRequired = true)]
        public byte[] Data { get; set; }

    }

}

#pragma warning restore CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
#endregion
