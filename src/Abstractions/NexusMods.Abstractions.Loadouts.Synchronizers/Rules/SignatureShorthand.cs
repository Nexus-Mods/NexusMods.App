// ReSharper disable InconsistentNaming
namespace NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

/// <summary>
/// Summary of all 92 possible signatures in a somewhat readable format
/// </summary>
public enum SignatureShorthand : ushort
{
	/// <summary>
	/// LoadoutExists
	/// </summary>
	xxA_xxx_i = 0x0004,
	/// <summary>
	/// LoadoutExists, LoadoutArchived
	/// </summary>
	xxA_xxX_i = 0x0104,
	/// <summary>
	/// LoadoutExists, PathIsIgnored
	/// </summary>
	xxA_xxx_I = 0x0204,
	/// <summary>
	/// LoadoutExists, LoadoutArchived, PathIsIgnored
	/// </summary>
	xxA_xxX_I = 0x0304,
	/// <summary>
	/// PrevExists
	/// </summary>
	xAx_xxx_i = 0x0002,
	/// <summary>
	/// PrevExists, PrevArchived
	/// </summary>
	xAx_xXx_i = 0x0082,
	/// <summary>
	/// PrevExists, PathIsIgnored
	/// </summary>
	xAx_xxx_I = 0x0202,
	/// <summary>
	/// PrevExists, PrevArchived, PathIsIgnored
	/// </summary>
	xAx_xXx_I = 0x0282,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevEqualsLoadout
	/// </summary>
	xAA_xxx_i = 0x0016,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevEqualsLoadout, PrevArchived, LoadoutArchived
	/// </summary>
	xAA_xXX_i = 0x0196,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevEqualsLoadout, PathIsIgnored
	/// </summary>
	xAA_xxx_I = 0x0216,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevEqualsLoadout, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	xAA_xXX_I = 0x0396,
	/// <summary>
	/// PrevExists, LoadoutExists
	/// </summary>
	xAB_xxx_i = 0x0006,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevArchived
	/// </summary>
	xAB_xXx_i = 0x0086,
	/// <summary>
	/// PrevExists, LoadoutExists, LoadoutArchived
	/// </summary>
	xAB_xxX_i = 0x0106,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevArchived, LoadoutArchived
	/// </summary>
	xAB_xXX_i = 0x0186,
	/// <summary>
	/// PrevExists, LoadoutExists, PathIsIgnored
	/// </summary>
	xAB_xxx_I = 0x0206,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevArchived, PathIsIgnored
	/// </summary>
	xAB_xXx_I = 0x0286,
	/// <summary>
	/// PrevExists, LoadoutExists, LoadoutArchived, PathIsIgnored
	/// </summary>
	xAB_xxX_I = 0x0306,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	xAB_xXX_I = 0x0386,
	/// <summary>
	/// DiskExists
	/// </summary>
	Axx_xxx_i = 0x0001,
	/// <summary>
	/// DiskExists, DiskArchived
	/// </summary>
	Axx_Xxx_i = 0x0041,
	/// <summary>
	/// DiskExists, PathIsIgnored
	/// </summary>
	Axx_xxx_I = 0x0201,
	/// <summary>
	/// DiskExists, DiskArchived, PathIsIgnored
	/// </summary>
	Axx_Xxx_I = 0x0241,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskEqualsLoadout
	/// </summary>
	AxA_xxx_i = 0x0025,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, LoadoutArchived
	/// </summary>
	AxA_XxX_i = 0x0165,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskEqualsLoadout, PathIsIgnored
	/// </summary>
	AxA_xxx_I = 0x0225,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	AxA_XxX_I = 0x0365,
	/// <summary>
	/// DiskExists, LoadoutExists
	/// </summary>
	AxB_xxx_i = 0x0005,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskArchived
	/// </summary>
	AxB_Xxx_i = 0x0045,
	/// <summary>
	/// DiskExists, LoadoutExists, LoadoutArchived
	/// </summary>
	AxB_xxX_i = 0x0105,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskArchived, LoadoutArchived
	/// </summary>
	AxB_XxX_i = 0x0145,
	/// <summary>
	/// DiskExists, LoadoutExists, PathIsIgnored
	/// </summary>
	AxB_xxx_I = 0x0205,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskArchived, PathIsIgnored
	/// </summary>
	AxB_Xxx_I = 0x0245,
	/// <summary>
	/// DiskExists, LoadoutExists, LoadoutArchived, PathIsIgnored
	/// </summary>
	AxB_xxX_I = 0x0305,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	AxB_XxX_I = 0x0345,
	/// <summary>
	/// DiskExists, PrevExists, DiskEqualsPrev
	/// </summary>
	AAx_xxx_i = 0x000B,
	/// <summary>
	/// DiskExists, PrevExists, DiskEqualsPrev, DiskArchived, PrevArchived
	/// </summary>
	AAx_XXx_i = 0x00CB,
	/// <summary>
	/// DiskExists, PrevExists, DiskEqualsPrev, PathIsIgnored
	/// </summary>
	AAx_xxx_I = 0x020B,
	/// <summary>
	/// DiskExists, PrevExists, DiskEqualsPrev, DiskArchived, PrevArchived, PathIsIgnored
	/// </summary>
	AAx_XXx_I = 0x02CB,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, PrevEqualsLoadout, DiskEqualsLoadout
	/// </summary>
	AAA_xxx_i = 0x003F,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, PrevEqualsLoadout, DiskEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived
	/// </summary>
	AAA_XXX_i = 0x01FF,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, PrevEqualsLoadout, DiskEqualsLoadout, PathIsIgnored
	/// </summary>
	AAA_xxx_I = 0x023F,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, PrevEqualsLoadout, DiskEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	AAA_XXX_I = 0x03FF,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev
	/// </summary>
	AAB_xxx_i = 0x000F,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, DiskArchived, PrevArchived
	/// </summary>
	AAB_XXx_i = 0x00CF,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, LoadoutArchived
	/// </summary>
	AAB_xxX_i = 0x010F,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, DiskArchived, PrevArchived, LoadoutArchived
	/// </summary>
	AAB_XXX_i = 0x01CF,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, PathIsIgnored
	/// </summary>
	AAB_xxx_I = 0x020F,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, DiskArchived, PrevArchived, PathIsIgnored
	/// </summary>
	AAB_XXx_I = 0x02CF,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, LoadoutArchived, PathIsIgnored
	/// </summary>
	AAB_xxX_I = 0x030F,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, DiskArchived, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	AAB_XXX_I = 0x03CF,
	/// <summary>
	/// DiskExists, PrevExists
	/// </summary>
	ABx_xxx_i = 0x0003,
	/// <summary>
	/// DiskExists, PrevExists, DiskArchived
	/// </summary>
	ABx_Xxx_i = 0x0043,
	/// <summary>
	/// DiskExists, PrevExists, PrevArchived
	/// </summary>
	ABx_xXx_i = 0x0083,
	/// <summary>
	/// DiskExists, PrevExists, DiskArchived, PrevArchived
	/// </summary>
	ABx_XXx_i = 0x00C3,
	/// <summary>
	/// DiskExists, PrevExists, PathIsIgnored
	/// </summary>
	ABx_xxx_I = 0x0203,
	/// <summary>
	/// DiskExists, PrevExists, DiskArchived, PathIsIgnored
	/// </summary>
	ABx_Xxx_I = 0x0243,
	/// <summary>
	/// DiskExists, PrevExists, PrevArchived, PathIsIgnored
	/// </summary>
	ABx_xXx_I = 0x0283,
	/// <summary>
	/// DiskExists, PrevExists, DiskArchived, PrevArchived, PathIsIgnored
	/// </summary>
	ABx_XXx_I = 0x02C3,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout
	/// </summary>
	ABA_xxx_i = 0x0027,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, LoadoutArchived
	/// </summary>
	ABA_XxX_i = 0x0167,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, PrevArchived
	/// </summary>
	ABA_xXx_i = 0x00A7,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived
	/// </summary>
	ABA_XXX_i = 0x01E7,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, PathIsIgnored
	/// </summary>
	ABA_xxx_I = 0x0227,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABA_XxX_I = 0x0367,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, PrevArchived, PathIsIgnored
	/// </summary>
	ABA_xXx_I = 0x02A7,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABA_XXX_I = 0x03E7,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout
	/// </summary>
	ABB_xxx_i = 0x0017,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, DiskArchived
	/// </summary>
	ABB_Xxx_i = 0x0057,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, PrevArchived, LoadoutArchived
	/// </summary>
	ABB_xXX_i = 0x0197,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived
	/// </summary>
	ABB_XXX_i = 0x01D7,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, PathIsIgnored
	/// </summary>
	ABB_xxx_I = 0x0217,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, DiskArchived, PathIsIgnored
	/// </summary>
	ABB_Xxx_I = 0x0257,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABB_xXX_I = 0x0397,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABB_XXX_I = 0x03D7,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists
	/// </summary>
	ABC_xxx_i = 0x0007,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived
	/// </summary>
	ABC_Xxx_i = 0x0047,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevArchived
	/// </summary>
	ABC_xXx_i = 0x0087,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, LoadoutArchived
	/// </summary>
	ABC_xxX_i = 0x0107,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, PrevArchived
	/// </summary>
	ABC_XXx_i = 0x00C7,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, LoadoutArchived
	/// </summary>
	ABC_XxX_i = 0x0147,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevArchived, LoadoutArchived
	/// </summary>
	ABC_xXX_i = 0x0187,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, PrevArchived, LoadoutArchived
	/// </summary>
	ABC_XXX_i = 0x01C7,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PathIsIgnored
	/// </summary>
	ABC_xxx_I = 0x0207,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, PathIsIgnored
	/// </summary>
	ABC_Xxx_I = 0x0247,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevArchived, PathIsIgnored
	/// </summary>
	ABC_xXx_I = 0x0287,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABC_xxX_I = 0x0307,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, PrevArchived, PathIsIgnored
	/// </summary>
	ABC_XXx_I = 0x02C7,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABC_XxX_I = 0x0347,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABC_xXX_I = 0x0387,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABC_XXX_I = 0x03C7,
}
