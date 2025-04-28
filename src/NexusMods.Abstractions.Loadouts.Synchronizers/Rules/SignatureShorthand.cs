// ReSharper disable InconsistentNaming
namespace NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

/// <summary>
/// Summary of all 92 possible <see cref="Signature"/> combinations in a somewhat readable format.
/// </summary>
public enum SignatureShorthand : ushort
{
	/// <summary>
	/// File exists only in loadout.
	/// File is not archived.
	/// </summary>
	xxA_xxx_i = Signature.LoadoutExists,
	/// <summary>
	/// File exists only in loadout.
	/// File is archived.
	/// </summary>
	xxA_xxX_i = Signature.LoadoutExists | Signature.LoadoutArchived,
	/// <summary>
	/// File exists only in loadout.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	xxA_xxx_I = Signature.LoadoutExists | Signature.PathIsIgnored,
	/// <summary>
	/// File exists only in loadout.
	/// File is archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	xxA_xxX_I = Signature.LoadoutExists | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists only in previous state.
	/// File is not archived.
	/// </summary>
	xAx_xxx_i = Signature.PrevExists,
	/// <summary>
	/// File exists only in previous state.
	/// File is archived.
	/// </summary>
	xAx_xXx_i = Signature.PrevExists | Signature.PrevArchived,
	/// <summary>
	/// File exists only in previous state.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	xAx_xxx_I = Signature.PrevExists | Signature.PathIsIgnored,
	/// <summary>
	/// File exists only in previous state.
	/// File is archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	xAx_xXx_I = Signature.PrevExists | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and previous state. Both hashes match.
	/// File is not archived.
	/// </summary>
	xAA_xxx_i = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout,
	/// <summary>
	/// File exists in loadout and previous state. Both hashes match.
	/// File is archived for both sources.
	/// </summary>
	xAA_xXX_i = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout and previous state. Both hashes match.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	xAA_xxx_I = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and previous state. Both hashes match.
	/// File is archived for both sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	xAA_xXX_I = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and previous state but hashes for both of them are different.
	/// File is not archived.
	/// </summary>
	xAB_xxx_i = Signature.PrevExists | Signature.LoadoutExists,
	/// <summary>
	/// File exists in loadout and previous state but hashes for both of them are different.
	/// File is archived for previous state.
	/// </summary>
	xAB_xXx_i = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived,
	/// <summary>
	/// File exists in loadout and previous state but hashes for both of them are different.
	/// File is archived for loadout.
	/// </summary>
	xAB_xxX_i = Signature.PrevExists | Signature.LoadoutExists | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout and previous state but hashes for both of them are different.
	/// File is archived for both sources.
	/// </summary>
	xAB_xXX_i = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout and previous state but hashes for both of them are different.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	xAB_xxx_I = Signature.PrevExists | Signature.LoadoutExists | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and previous state but hashes for both of them are different.
	/// File is archived for previous state.
	/// Path is on game-specific ingnore list.
	/// </summary>
	xAB_xXx_I = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and previous state but hashes for both of them are different.
	/// File is archived for loadout.
	/// Path is on game-specific ingnore list.
	/// </summary>
	xAB_xxX_I = Signature.PrevExists | Signature.LoadoutExists | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and previous state but hashes for both of them are different.
	/// File is archived for both sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	xAB_xXX_I = Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists only on disk.
	/// File is not archived.
	/// </summary>
	Axx_xxx_i = Signature.DiskExists,
	/// <summary>
	/// File exists only on disk.
	/// File is archived.
	/// </summary>
	Axx_Xxx_i = Signature.DiskExists | Signature.DiskArchived,
	/// <summary>
	/// File exists only on disk.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	Axx_xxx_I = Signature.DiskExists | Signature.PathIsIgnored,
	/// <summary>
	/// File exists only on disk.
	/// File is archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	Axx_Xxx_I = Signature.DiskExists | Signature.DiskArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and disk. Both hashes match.
	/// File is not archived.
	/// </summary>
	AxA_xxx_i = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout,
	/// <summary>
	/// File exists in loadout and disk. Both hashes match.
	/// File is archived for both sources.
	/// </summary>
	AxA_XxX_i = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout and disk. Both hashes match.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AxA_xxx_I = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and disk. Both hashes match.
	/// File is archived for both sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AxA_XxX_I = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and disk but hashes for both of them are different.
	/// File is not archived.
	/// </summary>
	AxB_xxx_i = Signature.DiskExists | Signature.LoadoutExists,
	/// <summary>
	/// File exists in loadout and disk but hashes for both of them are different.
	/// File is archived for disk.
	/// </summary>
	AxB_Xxx_i = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskArchived,
	/// <summary>
	/// File exists in loadout and disk but hashes for both of them are different.
	/// File is archived for loadout.
	/// </summary>
	AxB_xxX_i = Signature.DiskExists | Signature.LoadoutExists | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout and disk but hashes for both of them are different.
	/// File is archived for both sources.
	/// </summary>
	AxB_XxX_i = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout and disk but hashes for both of them are different.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AxB_xxx_I = Signature.DiskExists | Signature.LoadoutExists | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and disk but hashes for both of them are different.
	/// File is archived for disk.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AxB_Xxx_I = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and disk but hashes for both of them are different.
	/// File is archived for loadout.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AxB_xxX_I = Signature.DiskExists | Signature.LoadoutExists | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout and disk but hashes for both of them are different.
	/// File is archived for both sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AxB_XxX_I = Signature.DiskExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists on disk and previous state. Both hashes match.
	/// File is not archived.
	/// </summary>
	AAx_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.DiskEqualsPrev,
	/// <summary>
	/// File exists on disk and previous state. Both hashes match.
	/// File is archived for both sources.
	/// </summary>
	AAx_XXx_i = Signature.DiskExists | Signature.PrevExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived,
	/// <summary>
	/// File exists on disk and previous state. Both hashes match.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AAx_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.DiskEqualsPrev | Signature.PathIsIgnored,
	/// <summary>
	/// File exists on disk and previous state. Both hashes match.
	/// File is archived for both sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AAx_XXx_I = Signature.DiskExists | Signature.PrevExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state. All hashes match.
	/// File is not archived.
	/// </summary>
	AAA_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.PrevEqualsLoadout | Signature.DiskEqualsLoadout,
	/// <summary>
	/// File exists in loadout, disk and previous state. All hashes match.
	/// File is archived for all sources.
	/// </summary>
	AAA_XXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.PrevEqualsLoadout | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state. All hashes match.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AAA_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.PrevEqualsLoadout | Signature.DiskEqualsLoadout | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state. All hashes match.
	/// File is archived for all sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AAA_XXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.PrevEqualsLoadout | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for loadout differs from disk and previous state.
	/// File is not archived.
	/// </summary>
	AAB_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for loadout differs from disk and previous state.
	/// File is archived for previous state and disk.
	/// </summary>
	AAB_XXx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for loadout differs from disk and previous state.
	/// File is archived for loadout.
	/// </summary>
	AAB_xxX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for loadout differs from disk and previous state.
	/// File is archived for all sources.
	/// </summary>
	AAB_XXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for loadout differs from disk and previous state.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AAB_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for loadout differs from disk and previous state.
	/// File is archived for previous state and disk.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AAB_XXx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for loadout differs from disk and previous state.
	/// File is archived for loadout.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AAB_xxX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for loadout differs from disk and previous state.
	/// File is archived for all sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	AAB_XXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsPrev | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists on disk and previous state but hashes for both of them are different.
	/// File is not archived.
	/// </summary>
	ABx_xxx_i = Signature.DiskExists | Signature.PrevExists,
	/// <summary>
	/// File exists on disk and previous state but hashes for both of them are different.
	/// File is archived for disk.
	/// </summary>
	ABx_Xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.DiskArchived,
	/// <summary>
	/// File exists on disk and previous state but hashes for both of them are different.
	/// File is archived for previous state.
	/// </summary>
	ABx_xXx_i = Signature.DiskExists | Signature.PrevExists | Signature.PrevArchived,
	/// <summary>
	/// File exists on disk and previous state but hashes for both of them are different.
	/// File is archived for both sources.
	/// </summary>
	ABx_XXx_i = Signature.DiskExists | Signature.PrevExists | Signature.DiskArchived | Signature.PrevArchived,
	/// <summary>
	/// File exists on disk and previous state but hashes for both of them are different.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABx_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.PathIsIgnored,
	/// <summary>
	/// File exists on disk and previous state but hashes for both of them are different.
	/// File is archived for disk.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABx_Xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.DiskArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists on disk and previous state but hashes for both of them are different.
	/// File is archived for previous state.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABx_xXx_I = Signature.DiskExists | Signature.PrevExists | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists on disk and previous state but hashes for both of them are different.
	/// File is archived for both sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABx_XXx_I = Signature.DiskExists | Signature.PrevExists | Signature.DiskArchived | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for previous state differs from loadout and disk.
	/// File is not archived.
	/// </summary>
	ABA_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for previous state differs from loadout and disk.
	/// File is archived for disk and loadout.
	/// </summary>
	ABA_XxX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for previous state differs from loadout and disk.
	/// File is archived for previous state.
	/// </summary>
	ABA_xXx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.PrevArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for previous state differs from loadout and disk.
	/// File is archived for all sources.
	/// </summary>
	ABA_XXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for previous state differs from loadout and disk.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABA_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for previous state differs from loadout and disk.
	/// File is archived for disk and loadout.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABA_XxX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for previous state differs from loadout and disk.
	/// File is archived for previous state.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABA_xXx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for previous state differs from loadout and disk.
	/// File is archived for all sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABA_XXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for disk differs from loadout and previous state.
	/// File is not archived.
	/// </summary>
	ABB_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for disk differs from loadout and previous state.
	/// File is archived for disk.
	/// </summary>
	ABB_Xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.DiskArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for disk differs from loadout and previous state.
	/// File is archived for previous state and loadout.
	/// </summary>
	ABB_xXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for disk differs from loadout and previous state.
	/// File is archived for all sources.
	/// </summary>
	ABB_XXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for disk differs from loadout and previous state.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABB_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for disk differs from loadout and previous state.
	/// File is archived for disk.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABB_Xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.DiskArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for disk differs from loadout and previous state.
	/// File is archived for previous state and loadout.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABB_xXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hash for disk differs from loadout and previous state.
	/// File is archived for all sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABB_XXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevEqualsLoadout | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is not archived.
	/// </summary>
	ABC_xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for disk.
	/// </summary>
	ABC_Xxx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for previous state.
	/// </summary>
	ABC_xXx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for loadout.
	/// </summary>
	ABC_xxX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for previous state and disk.
	/// </summary>
	ABC_XXx_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PrevArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for disk and loadout.
	/// </summary>
	ABC_XxX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for previous state and loadout.
	/// </summary>
	ABC_xXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for all sources.
	/// </summary>
	ABC_XXX_i = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is not archived.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABC_xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for disk.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABC_Xxx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for previous state.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABC_xXx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for loadout.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABC_xxX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for previous state and disk.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABC_XXx_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PrevArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for disk and loadout.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABC_XxX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for previous state and loadout.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABC_xXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
	/// <summary>
	/// File exists in loadout, disk and previous state but hashes for all of them are different.
	/// File is archived for all sources.
	/// Path is on game-specific ingnore list.
	/// </summary>
	ABC_XXX_I = Signature.DiskExists | Signature.PrevExists | Signature.LoadoutExists | Signature.DiskArchived | Signature.PrevArchived | Signature.LoadoutArchived | Signature.PathIsIgnored,
}
