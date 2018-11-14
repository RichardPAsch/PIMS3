using PIMS3.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PIMS3.Data.Repositories.ImportData
{
    interface IImportDataRepository
    {
        DataImportVm SaveRevenue(DataImportVm fileToImport);
    }

}

    