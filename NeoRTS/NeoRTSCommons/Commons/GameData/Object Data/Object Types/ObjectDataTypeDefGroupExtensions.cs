using System;
using System.Collections.Generic;
using System.Reflection;
namespace NeoRTS
{
    namespace GameData
    {
        namespace ObjectData
        {
            namespace Types
            {
                public static class ObjectDataTypeDefGroupExtensions
                {
                    /// <summary>
                    /// Returns a new ObjectDataTypeDef array that contains all data types defined
                    /// within both passed arrays. If the same type is found in both arrays, it
                    /// will be present only once in the resulting array with b's field values used in priority.
                    /// 
                    /// Effectively means "add b to a, overwrite common elements with b's element".
                    /// </summary>
                    static public ObjectDataTypeDef[] Overlap(this ObjectDataTypeDef[] a, ObjectDataTypeDef[] b)
                    {
                        List<ObjectDataTypeDef> finalDataTypeDefs = new List<ObjectDataTypeDef>();

                        foreach(var defA in a)
                        {
                            ObjectDataTypeDef final = defA;
                            foreach(var defB in b)
                            {
                                if (defB.DataType == defA.DataType)
                                {
                                    final = new ObjectDataTypeDef(defA.DataType, defB.DefaultValue);
                                }
                            }
                            finalDataTypeDefs.Add(final);
                        }

                        foreach(var defB in b)
                        {
                            bool presentInFinalList = false;
                            foreach(var defFinal in finalDataTypeDefs)
                            {
                                if (defFinal.DataType == defB.DataType)
                                {
                                    presentInFinalList = true;
                                    break;
                                }
                            }

                            if (!presentInFinalList)
                            {
                                finalDataTypeDefs.Add(defB);
                            }
                        }

                        return finalDataTypeDefs.ToArray();

                    }
                }
            }
        }
    }
}

