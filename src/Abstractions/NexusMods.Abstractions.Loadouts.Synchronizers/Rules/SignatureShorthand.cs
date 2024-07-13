// ReSharper disable InconsistentNaming
namespace NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

/// <summary>
/// Summary of all 92 possible <see cref="Signature"/> combinations in a somewhat readable format.
/// </summary>
public enum SignatureShorthand : ushort
{
	/// <summary>
	/// LoadoutExists
	/// </summary>
	xxA_xxx_i = Signature.LoadoutExists,
	/// <summary>
	/// LoadoutExists, LoadoutArchived
	/// </summary>
	xxA_xxX_i = Signature.LoadoutExists | Signature.LoadoutArchived,
	/// <summary>
	/// LoadoutExists, PathIsIgnored
	/// </summary>
	xxA_xxx_I = Signature.LoadoutExists | Signature.PathIsIgnored,
	/// <summary>
	/// LoadoutExists, LoadoutArchived, PathIsIgnored
	/// </summary>
	xxA_xxX_I = Signature.LoadoutExists | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// PrevExists
	/// </summary>
	xAx_xxx_i = Signature.PrevExists,
	/// <summary>
	/// PrevExists, PrevArchived
	/// </summary>
	xAx_xXx_i = Signature.PrevExists | Signature.PrevArchived,
	/// <summary>
	/// PrevExists, PathIsIgnored
	/// </summary>
	xAx_xxx_I = Signature.PrevExists | Signature.PathIsIgnored,
	/// <summary>
	/// PrevExists, PrevArchived, PathIsIgnored
	/// </summary>
	xAx_xXx_I = Signature.PrevExists | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevEqualsLoadout
	/// </summary>
	xAA_xxx_i = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevEqualsLoadout, PrevArchived, LoadoutArchived
	/// </summary>
	xAA_xXX_i = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevEqualsLoadout, PathIsIgnored
	/// </summary>
	xAA_xxx_I = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PathIsIgnored,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevEqualsLoadout, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	xAA_xXX_I = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// PrevExists, LoadoutExists
	/// </summary>
	xAB_xxx_i = Signature.PrevExists | Signature.LoadoutExists,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevArchived
	/// </summary>
	xAB_xXx_i = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived,
	/// <summary>
	/// PrevExists, LoadoutExists, LoadoutArchived
	/// </summary>
	xAB_xxX_i = Signature.PrevExists | Signature.LoadoutExists | Signature.LoadoutArchived,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevArchived, LoadoutArchived
	/// </summary>
	xAB_xXX_i = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// PrevExists, LoadoutExists, PathIsIgnored
	/// </summary>
	xAB_xxx_I = Signature.PrevExists | Signature.LoadoutExists | Signature.PathIsIgnored,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevArchived, PathIsIgnored
	/// </summary>
	xAB_xXx_I = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// PrevExists, LoadoutExists, LoadoutArchived, PathIsIgnored
	/// </summary>
	xAB_xxX_I = Signature.PrevExists | Signature.LoadoutExists | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// PrevExists, LoadoutExists, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	xAB_xXX_I = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists
	/// </summary>
	Axx_xxx_i = Signature.DiskExists,
	/// <summary>
	/// DiskExists, DiskArchived
	/// </summary>
	Axx_Xxx_i = Signature.DiskExists | Signature.DiskArchived,
	/// <summary>
	/// DiskExists, PathIsIgnored
	/// </summary>
	Axx_xxx_I = Signature.DiskExists | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, DiskArchived, PathIsIgnored
	/// </summary>
	Axx_Xxx_I = Signature.DiskExists | Signature.DiskArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskEqualsLoadout
	/// </summary>
	AxA_xxx_i = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, LoadoutArchived
	/// </summary>
	AxA_XxX_i = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskEqualsLoadout, PathIsIgnored
	/// </summary>
	AxA_xxx_I = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	AxA_XxX_I = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, LoadoutExists
	/// </summary>
	AxB_xxx_i = Signature.DiskExists | Signature.LoadoutExists,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskArchived
	/// </summary>
	AxB_Xxx_i = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskArchived,
	/// <summary>
	/// DiskExists, LoadoutExists, LoadoutArchived
	/// </summary>
	AxB_xxX_i = Signature.DiskExists | Signature.LoadoutExists | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskArchived, LoadoutArchived
	/// </summary>
	AxB_XxX_i = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, LoadoutExists, PathIsIgnored
	/// </summary>
	AxB_xxx_I = Signature.DiskExists | Signature.LoadoutExists | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskArchived, PathIsIgnored
	/// </summary>
	AxB_Xxx_I = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, LoadoutExists, LoadoutArchived, PathIsIgnored
	/// </summary>
	AxB_xxX_I = Signature.DiskExists | Signature.LoadoutExists | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, LoadoutExists, DiskArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	AxB_XxX_I = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, DiskEqualsPrev
	/// </summary>
	AAx_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.DiskEqualsPrev,
	/// <summary>
	/// DiskExists, PrevExists, DiskEqualsPrev, DiskArchived, PrevArchived
	/// </summary>
	AAx_XXx_i = Signature.DiskExists | Signature.PrevExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived,
	/// <summary>
	/// DiskExists, PrevExists, DiskEqualsPrev, PathIsIgnored
	/// </summary>
	AAx_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.DiskEqualsPrev | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, DiskEqualsPrev, DiskArchived, PrevArchived, PathIsIgnored
	/// </summary>
	AAx_XXx_I = Signature.DiskExists | Signature.PrevExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, PrevEqualsLoadout, DiskEqualsLoadout
	/// </summary>
	AAA_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.PrevEqualsLoadout | Signature.DiskEqualsLoadout,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, PrevEqualsLoadout, DiskEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived
	/// </summary>
	AAA_XXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.PrevEqualsLoadout | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, PrevEqualsLoadout, DiskEqualsLoadout, PathIsIgnored
	/// </summary>
	AAA_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.PrevEqualsLoadout | Signature.DiskEqualsLoadout | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, PrevEqualsLoadout, DiskEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	AAA_XXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.PrevEqualsLoadout | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev
	/// </summary>
	AAB_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, DiskArchived, PrevArchived
	/// </summary>
	AAB_XXx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, LoadoutArchived
	/// </summary>
	AAB_xxX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, DiskArchived, PrevArchived, LoadoutArchived
	/// </summary>
	AAB_XXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, PathIsIgnored
	/// </summary>
	AAB_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, DiskArchived, PrevArchived, PathIsIgnored
	/// </summary>
	AAB_XXx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, LoadoutArchived, PathIsIgnored
	/// </summary>
	AAB_xxX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsPrev, DiskArchived, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	AAB_XXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists
	/// </summary>
	ABx_xxx_i = Signature.DiskExists | Signature.PrevExists,
	/// <summary>
	/// DiskExists, PrevExists, DiskArchived
	/// </summary>
	ABx_Xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.DiskArchived,
	/// <summary>
	/// DiskExists, PrevExists, PrevArchived
	/// </summary>
	ABx_xXx_i = Signature.DiskExists | Signature.PrevExists | Signature.PrevArchived,
	/// <summary>
	/// DiskExists, PrevExists, DiskArchived, PrevArchived
	/// </summary>
	ABx_XXx_i = Signature.DiskExists | Signature.PrevExists | Signature.DiskArchived | Signature.PrevArchived,
	/// <summary>
	/// DiskExists, PrevExists, PathIsIgnored
	/// </summary>
	ABx_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, DiskArchived, PathIsIgnored
	/// </summary>
	ABx_Xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.DiskArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, PrevArchived, PathIsIgnored
	/// </summary>
	ABx_xXx_I = Signature.DiskExists | Signature.PrevExists | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, DiskArchived, PrevArchived, PathIsIgnored
	/// </summary>
	ABx_XXx_I = Signature.DiskExists | Signature.PrevExists | Signature.DiskArchived | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout
	/// </summary>
	ABA_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, LoadoutArchived
	/// </summary>
	ABA_XxX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, PrevArchived
	/// </summary>
	ABA_xXx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.PrevArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived
	/// </summary>
	ABA_XXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, PathIsIgnored
	/// </summary>
	ABA_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABA_XxX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, PrevArchived, PathIsIgnored
	/// </summary>
	ABA_xXx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABA_XXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout
	/// </summary>
	ABB_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, DiskArchived
	/// </summary>
	ABB_Xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.DiskArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, PrevArchived, LoadoutArchived
	/// </summary>
	ABB_xXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived
	/// </summary>
	ABB_XXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, PathIsIgnored
	/// </summary>
	ABB_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, DiskArchived, PathIsIgnored
	/// </summary>
	ABB_Xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.DiskArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABB_xXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevEqualsLoadout, DiskArchived, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABB_XXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists
	/// </summary>
	ABC_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived
	/// </summary>
	ABC_Xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevArchived
	/// </summary>
	ABC_xXx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, LoadoutArchived
	/// </summary>
	ABC_xxX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, PrevArchived
	/// </summary>
	ABC_XXx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PrevArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, LoadoutArchived
	/// </summary>
	ABC_XxX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevArchived, LoadoutArchived
	/// </summary>
	ABC_xXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, PrevArchived, LoadoutArchived
	/// </summary>
	ABC_XXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PathIsIgnored
	/// </summary>
	ABC_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, PathIsIgnored
	/// </summary>
	ABC_Xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevArchived, PathIsIgnored
	/// </summary>
	ABC_xXx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABC_xxX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, PrevArchived, PathIsIgnored
	/// </summary>
	ABC_XXx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABC_XxX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABC_xXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// DiskExists, PrevExists, LoadoutExists, DiskArchived, PrevArchived, LoadoutArchived, PathIsIgnored
	/// </summary>
	ABC_XXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
}
