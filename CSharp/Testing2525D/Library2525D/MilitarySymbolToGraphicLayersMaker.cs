﻿/* 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library2525D
{
    /// <summary>
    /// General processing is to convert a Symbol Id Code into a list of strings
    /// These strings represent the paths to the set of images/layers that *should*
    /// represent that Symbol Id
    /// For convenience, it take a MilitarySymbol object and sets the GraphicLayers to these strings
    /// </summary>
    public class MilitarySymbolToGraphicLayersMaker
    {
        // IMPORTANT: If you don't have the expected SVG Files, in the expected folder format
        //            then this class will not do anything.
        // The incomplete SVG snapshot: "2525D_SVG_PNG_062013" was used.
        // The assumed/expected Folder structure:
        // {ImageFilesHome} <--- SEE DEFINITION BELOW
        //  |- Echelon
        //  |- Frames
        //  |- Headquarters
        //  |- Appendices
        //     |- Air
        //     |- Control Measures
        //     |- Cyberspace
        //     |- Land
        // (etc.)

        // TODO: IMPORTANT: You Must Set this to the location on your machine
        // Note: PathSeparator on the end (required/assumed)
        public static readonly string ImageFilesHome = @"[!!!!!!!!!!!SET_THIS_FOLDER_!!!!!!!!!!!]\2525D_SVG_PNG_062013"
            + System.IO.Path.DirectorySeparatorChar;

        const string ImageSuffix = ".svg";

        public static bool SetMilitarySymbolGraphicLayers(ref MilitarySymbol milSymbol)
        {
            if ((milSymbol == null) || (milSymbol.Id == null) ||
                (milSymbol.GraphicLayers == null) || (!milSymbol.Id.IsValid))
                return false;

            milSymbol.GraphicLayers.Clear();

            // Frame Layer 
            // = StandardIdentityAffiliationType + SymbolSetType + "(affiliation)"
            // ex. 0520(hostile)
            StringBuilder sb = new StringBuilder();

            sb.Append(ImageFilesHome);
            sb.Append("Frames");
            sb.Append(System.IO.Path.DirectorySeparatorChar);
            sb.Append(TypeUtilities.EnumHelper.getEnumValAsString(milSymbol.Id.Affiliation, 2));
            sb.Append(TypeUtilities.EnumHelper.getEnumValAsString(milSymbol.Id.SymbolSet, 2));
            string affilName = TypeUtilities.AffiliationTypeToImageName[milSymbol.Id.Affiliation];
            sb.Append("(" + affilName + ")");
            sb.Append(ImageSuffix);

            milSymbol.GraphicLayers.Add(sb.ToString());

            // Main Icon Layer
            // {SymbolSetTypeName}\SymbolSetType + EntityCode 

            sb.Clear();
            sb.Append(ImageFilesHome);
            sb.Append("Appendices");
            sb.Append(System.IO.Path.DirectorySeparatorChar);

            string symbolSetSubFolderName = string.Empty;
            if (TypeUtilities.SymbolSetToFolderName.ContainsKey(milSymbol.Id.SymbolSet))
                symbolSetSubFolderName = TypeUtilities.SymbolSetToFolderName[milSymbol.Id.SymbolSet];

            sb.Append(symbolSetSubFolderName);
            sb.Append(System.IO.Path.DirectorySeparatorChar);

            sb.Append(TypeUtilities.EnumHelper.getEnumValAsString(milSymbol.Id.SymbolSet, 2));
            sb.Append(milSymbol.Id.FullEntityCode);

            sb.Append(ImageSuffix);
            milSymbol.GraphicLayers.Add(sb.ToString());

            // 
            // TODO: Stop here for Control Measures & 
            //       Figure out which of the additional layers apply for which sets
            //

            // Main Icon Modfier 1

            // TODO

            // Main Icon Modfier 2

            // TODO

            // Echelon Modifier

            // TODO

            // Headquarters/TF/FD Modifier

            // TODO

            // Other?

            if (milSymbol.GraphicLayers.Count == 0)
                return false;
            else
                return true;

        }


    }
}