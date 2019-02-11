using PIMS3.Data.Repositories;

namespace PIMS3.Services
{
    public interface ICommonSvc
    {
        string GetInvestorIdFromInvestor(string currentInvestor);

        //string GenerateGuid();
    }

}