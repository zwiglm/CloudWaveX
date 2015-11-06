using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace PlasticWonderland
{
    public class TypeTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            LibraryRootObject typeItem = item as LibraryRootObject;
            if (typeItem != null)
            {
                if (typeItem.type.Equals(GlobalVariables.FILE_AS_FILE))
                {
                    return GetDataTemplate(GlobalVariables.TYPEFILE_AS_TYPEFILE, container);
                }
                else if (typeItem.type == "dir")
                {
                    return GetDataTemplate("type_dir", container);
                }
                else if ((typeItem.type == GlobalVariables.SF_RESP_REPOS) && (typeItem.encrypted == true))
                {
                    return GetDataTemplate("lib_crypt", container);
                }
                else if ((typeItem.type == GlobalVariables.SF_RESP_REPOS) && (typeItem.encrypted == false))
                {
                     return GetDataTemplate("lib_nocrypt", container);
                }
                else if ((typeItem.type == GlobalVariables.SHARED_REPO_HELPER) && (typeItem.encrypted == false))
                {
                    return GetDataTemplate("slib_nocrypt", container);
                }
                else if ((typeItem.type == GlobalVariables.SHARED_REPO_HELPER) && (typeItem.encrypted == true))
                {
                    return GetDataTemplate("slib_crypt", container);
                }
                else if (typeItem.type == GlobalVariables.GROUP_SPLITTER)
                {
                    return GetDataTemplate(GlobalVariables.GROUP_SPLITTER, container);
                }
                else
                {
                    return GetDataTemplate("NotDetermined", container);
                }
            }
            else
            {
                return base.SelectTemplate(item, container);
            }
        }
    }
}
