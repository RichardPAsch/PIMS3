 using PIMS3.Data.Entities;
 using PIMS3.DataAccess.Profile;

namespace PIMS3.BusinessLogic.ProfileData
{
    public class ProfileProcessing
    {
        Profile profileToBeInitialized;
        public Profile BuildProfileForProjections(Profile profile, Data.PIMS3Context ctx)
        {
            profileToBeInitialized = profile;
            var profileDataAccessComponent = new ProfileDataProcessing(ctx);

            // try-catch here ? 
            profileToBeInitialized = profileDataAccessComponent.BuildProfile(profile.TickerSymbol.Trim());
            if(profileToBeInitialized.DividendYield == 0M)
                CalculateDividendYield();

            return profileToBeInitialized;
        }

        private void CalculateDividendYield()
        {
            // TODO: ** duplicate of ImportFileProcessing.CalculateDividendYield(). **
            // Assumes dividend rates are on an ANNUAL basis.
            if(profileToBeInitialized.DividendRate > 0 && profileToBeInitialized.UnitPrice > 0)
                profileToBeInitialized.DividendYield = (profileToBeInitialized.DividendRate / profileToBeInitialized.UnitPrice) * 100;
            else
                profileToBeInitialized.DividendYield = 0M;
        }
        
    }
}
