using NeoRTS.GameData.ObjectData.Types;
using NeoRTS.GameData.Resources;
using NeoRTS.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            /// <summary>
            /// Data structure specialized in building data used in turn by the <see cref="ObjectDataTypeDatabase"/>.
            /// Requires <see cref="dataFilePath"/> to be filled in with the path, name and extension of
            /// a valid binary file that can be decoded as a <see cref="ARCHETYPE_FILE_DATA"/> structure followed
            /// by a <see cref="OBJECT_TYPES_FILE_DATA"/> structure.
            /// </summary>
            public struct ObjectTypesDatabaseBuilder
            {
                public string dataFilePath;
                public ObjectArchetype[] archetypesArray;
                public ObjectType[] objectTypesArray;

                public Type[] objectDataTypes;

                public unsafe void LoadDatabaseFromFile()
                {
                    FileStream dataFile = File.OpenRead(dataFilePath);
                    BinaryReader reader = new BinaryReader(dataFile);
                    ARCHETYPE_FILE_DATA archetypeFileData;
                    OBJECT_TYPES_FILE_DATA objectTypesFileData;
                    // ARCHETYPES LOADING
                    Debug.Log("Reading Archetypes from file...");
                    archetypeFileData = ARCHETYPE_FILE_DATA.ReadFrom(reader);
                    // OBJECT TYPES LOADING
                    Debug.Log("Reading Object Types from file...");
                    objectTypesFileData = OBJECT_TYPES_FILE_DATA.ReadFrom(reader);

                    // Parsing Archetype File Data into Database
                    {
                        archetypesArray = new ObjectArchetype[archetypeFileData.archetypes.Count];
                        for (int i = 0; i < archetypeFileData.archetypes.Count; i++)
                        {
                            ARCHETYPE_FILE_DATA.ARCHETYPE_DATA archetypeData = archetypeFileData.archetypes[i];
                            ObjectArchetype newArchetype;
                            List<ObjectDataTypeDef> newArchetypeDataDefsList = new List<ObjectDataTypeDef>();
                            foreach (var dataDef in archetypeData.dataDefs)
                            {
                                newArchetypeDataDefsList.Add(dataDef.BuildLoadedDataDef(objectDataTypes));
                            }

                            newArchetype = new ObjectArchetype(newArchetypeDataDefsList.ToArray());
                            archetypesArray[i] = newArchetype;
                        }
                    }

                    // Parsing Object Type File Data into Database
                    {
                        objectTypesArray = new ObjectType[objectTypesFileData.objectTypeCount];

                        for(int objectTypeID = 0; objectTypeID < objectTypesFileData.objectTypeCount; objectTypeID++)
                        {
                            ObjectArchetype archetype;
                            ObjectDataTypeDef[] overrides;
                            int actorID, pawnTypeID;
                            string name;

                            OBJECT_TYPES_FILE_DATA.OBJECT_TYPE_DATA objectTypeData = objectTypesFileData.objectTypes[objectTypeID];

                            archetype = archetypesArray[objectTypeData.archetypeID];
                            List<ObjectDataTypeDef> newObjectTypeDataDefsList = new List<ObjectDataTypeDef>();
                            foreach (var dataDef in objectTypeData.dataDefOverrides)
                            {
                                newObjectTypeDataDefsList.Add(dataDef.BuildLoadedDataDef(objectDataTypes));
                            }
                            overrides = newObjectTypeDataDefsList.ToArray();

                            actorID = objectTypeData.actorID;
                            pawnTypeID = objectTypeData.pawnTypeID;
                            name = new string(objectTypeData.name);

                            objectTypesArray[objectTypeID] = new ObjectType(archetype, overrides, actorID, pawnTypeID, name);
                        }
                    }

                    dataFile.Dispose();
                }
            }
        }
    }
}

