
using NeoRTS.GameData.ObjectData.Types;
using NeoRTS.Tools;
using System;
using System.Collections.Generic;
using System.IO;

namespace NeoRTS
{
    namespace GameData
    {
        namespace Resources
        {
            public struct OBJECT_DATA_TYPE_DEF_FILE_DATA
            {
                public int typeNameLength;
                public char[] typeName;
                public int fieldCount;
                public List<FIELD> fields;
                public struct FIELD
                {
                    public int fieldNameLength;
                    public char[] fieldName;
                    public int defaultValueSize;
                    public byte[] defaultValue;
                }

                public void ReadFromBinaryReader(BinaryReader reader)
                {
                    // Read type name
                    typeNameLength = reader.ReadInt32();
                    typeName = reader.ReadChars(typeNameLength);
                    fields = new List<FIELD>();
                    fieldCount = reader.ReadInt32();
                    for (int fieldID = 0; fieldID < fieldCount; fieldID++)
                    {
                        FIELD field = new OBJECT_DATA_TYPE_DEF_FILE_DATA.FIELD();

                        field.fieldNameLength = reader.ReadInt32();
                        field.fieldName = reader.ReadChars(field.fieldNameLength);
                        field.defaultValueSize = reader.ReadInt32();
                        field.defaultValue = reader.ReadBytes(field.defaultValueSize);

                        fields.Add(field);
                    }
                }

                public void WriteToBinaryWriter(BinaryWriter writer)
                {
                    writer.Write(typeNameLength);
                    writer.Write(typeName);
                    writer.Write(fieldCount);
                    foreach (var field in fields)
                    {
                        writer.Write(field.fieldNameLength);
                        writer.Write(field.fieldName);
                        writer.Write(field.defaultValueSize);
                        writer.Write(field.defaultValue);
                    }
                }

                public ObjectDataTypeDef BuildLoadedDataDef(Type[] objectDataTypes)
                {
                    ObjectDataTypeDef loaded = new ObjectDataTypeDef();
                    Type newDefType = null;
                    string typeNameStr = new string(typeName);

                    foreach (var type in objectDataTypes)
                    {
                        if (type.Name == typeNameStr)
                        {
                            newDefType = type;
                        }
                    }
                    if (newDefType == null) throw new Exception("Error while reading Archetype data from file : Found Data def type name that doesn't match any possible type.");

                    object defaultValue = Activator.CreateInstance(newDefType);
                    var allFields = newDefType.GetFields();
                    foreach (var field in allFields)
                    {
                        foreach (var readField in fields)
                        {
                            if (field.Name == new string(readField.fieldName))
                            {
                                object val = Tools.BytesReader.ReadFromBytes(readField.defaultValue, field.FieldType);
                                field.SetValue(defaultValue, val);
                            }
                        }
                    }
                    loaded = new ObjectDataTypeDef(newDefType, defaultValue);

                    return loaded;
                }
            }

            public struct ARCHETYPE_FILE_DATA
            {
                public struct ARCHETYPE_DATA
                {
                    public int dataDefsCount;
                    public List<OBJECT_DATA_TYPE_DEF_FILE_DATA> dataDefs;
                }

                public int archetypeCount;
                public List<ARCHETYPE_DATA> archetypes;

                static public ARCHETYPE_FILE_DATA ReadFrom(BinaryReader reader)
                {
                    // Construct archetype file data
                    var archetypeFileData = new ARCHETYPE_FILE_DATA();
                    archetypeFileData.archetypes = new List<ARCHETYPE_FILE_DATA.ARCHETYPE_DATA>();
                    archetypeFileData.archetypeCount = reader.ReadInt32();

                    // Read Archetypes
                    int archetypesRead = 0;
                    while (archetypesRead < archetypeFileData.archetypeCount)
                    {
                        // Read new Archetype
                        ARCHETYPE_FILE_DATA.ARCHETYPE_DATA archetypeData = new ARCHETYPE_FILE_DATA.ARCHETYPE_DATA();
                        archetypeData.dataDefsCount = reader.ReadInt32();
                        archetypeData.dataDefs = new List<OBJECT_DATA_TYPE_DEF_FILE_DATA>();

                        // Read Archetype Data Defs
                        int archetypeDataDefsRead = 0;
                        while (archetypeDataDefsRead < archetypeData.dataDefsCount)
                        {
                            // Read new Archetype Data Def
                            OBJECT_DATA_TYPE_DEF_FILE_DATA archetypeDataDef = new OBJECT_DATA_TYPE_DEF_FILE_DATA();
                            archetypeDataDef.ReadFromBinaryReader(reader);

                            archetypeData.dataDefs.Add(archetypeDataDef);
                            archetypeDataDefsRead++;
                        }

                        archetypeFileData.archetypes.Add(archetypeData);
                        archetypesRead++;
                    }
                    return archetypeFileData;
                }

                static public ARCHETYPE_FILE_DATA ReadFrom(Stream stream)
                {
                    BinaryReader reader = new BinaryReader(stream);

                    return ReadFrom(reader);
                }
                public void WriteToStream(Stream stream)
                {
                    BinaryWriter writer = new BinaryWriter(stream);

                    writer.Write(archetypeCount);
                    foreach(var archetype in archetypes)
                    {
                        writer.Write(archetype.dataDefsCount);
                        foreach (var dataDef in archetype.dataDefs)
                        {
                            dataDef.WriteToBinaryWriter(writer);
                        }
                    }
                }
            }

            public struct OBJECT_TYPES_FILE_DATA
            {
                public struct OBJECT_TYPE_DATA
                {
                    public int archetypeID;
                    public int actorID;
                    public int pawnTypeID;
                    public int nameLength;
                    public char[] name;
                    public int dataDefOverridesCount;
                    public List<OBJECT_DATA_TYPE_DEF_FILE_DATA> dataDefOverrides;
                }

                public int objectTypeCount;
                public List<OBJECT_TYPE_DATA> objectTypes;

                static public OBJECT_TYPES_FILE_DATA ReadFrom(BinaryReader reader)
                {
                    // Construct archetype file data
                    var objectTypesFileData = new OBJECT_TYPES_FILE_DATA();
                    objectTypesFileData.objectTypes = new List<OBJECT_TYPE_DATA>();
                    objectTypesFileData.objectTypeCount = reader.ReadInt32();

                    // Read Archetypes
                    int objectTypesRead = 0;
                    while (objectTypesRead < objectTypesFileData.objectTypeCount)
                    {
                        // Read new Object Type
                        OBJECT_TYPE_DATA objectTypeData = new OBJECT_TYPE_DATA();

                        objectTypeData.archetypeID = reader.ReadInt32();
                        objectTypeData.actorID = reader.ReadInt32();
                        objectTypeData.pawnTypeID = reader.ReadInt32();
                        objectTypeData.nameLength = reader.ReadInt32();
                        objectTypeData.name = reader.ReadChars(objectTypeData.nameLength);

                        objectTypeData.dataDefOverridesCount = reader.ReadInt32();
                        objectTypeData.dataDefOverrides = new List<OBJECT_DATA_TYPE_DEF_FILE_DATA>();

                        // Read Object Type override Data Defs
                        int archetypeDataDefsRead = 0;
                        while (archetypeDataDefsRead < objectTypeData.dataDefOverridesCount)
                        {
                            // Read new Object Type override Data Def
                            OBJECT_DATA_TYPE_DEF_FILE_DATA archetypeDataDef = new OBJECT_DATA_TYPE_DEF_FILE_DATA();
                            archetypeDataDef.ReadFromBinaryReader(reader);

                            objectTypeData.dataDefOverrides.Add(archetypeDataDef);
                            archetypeDataDefsRead++;
                        }

                        objectTypesFileData.objectTypes.Add(objectTypeData);
                        objectTypesRead++;
                    }
                    return objectTypesFileData;
                }

                static public OBJECT_TYPES_FILE_DATA ReadFrom(Stream stream)
                {
                    BinaryReader reader = new BinaryReader(stream);

                    return ReadFrom(reader);
                }
                public void WriteToStream(Stream stream)
                {
                    BinaryWriter writer = new BinaryWriter(stream);

                    writer.Write(objectTypeCount);
                    foreach (var objectType in objectTypes)
                    {
                        writer.Write(objectType.archetypeID);
                        writer.Write(objectType.actorID);
                        writer.Write(objectType.pawnTypeID);
                        writer.Write(objectType.nameLength);
                        writer.Write(objectType.name);

                        writer.Write(objectType.dataDefOverridesCount);
                        foreach (var dataDef in objectType.dataDefOverrides)
                        {
                            dataDef.WriteToBinaryWriter(writer);
                        }
                    }
                }
            }
        }
    }
}