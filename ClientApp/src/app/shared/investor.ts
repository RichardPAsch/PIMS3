export class Investor {

    investorId: string;
    loginName: string; 
    password: string;
    firstName: string;  // per Investor account
    lastName: string;   
    token?: string;
    role: string;
    passwordHash: string;
    passwordSalt: string;
}
