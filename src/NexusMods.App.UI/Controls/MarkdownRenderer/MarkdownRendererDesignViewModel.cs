using System.Reactive;
using Avalonia.Media;
using JetBrains.Annotations;
using Markdown.Avalonia.Utils;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.MarkdownRenderer;

public class MarkdownRendererDesignViewModel : AViewModel<IMarkdownRendererViewModel>, IMarkdownRendererViewModel
{
    public string Contents { get; set; }

    public ReactiveCommand<string, Unit> OpenLinkCommand { get; } = ReactiveCommand.Create<string>(_ => { });

    public IImageResolver ImageResolver => new ImageResolverImpl();
    public IPathResolver PathResolver => new PathResolverImpl();

    [UsedImplicitly]
    public MarkdownRendererDesignViewModel() : this(DefaultContents) { }

    public MarkdownRendererDesignViewModel(string contents)
    {
        Contents = contents;
    }

    private class PathResolverImpl : IPathResolver
    {
        public string? AssetPathRoot { get; set; }
        public IEnumerable<string>? CallerAssemblyNames { get; set; }

        public Task<Stream?>? ResolveImageResource(string relativeOrAbsolutePath)
        {
            throw new NotImplementedException();
        }
    }

    private class ImageResolverImpl : IImageResolver
    {
        public Task<IImage?> Load(Stream stream)
        {
            throw new NotImplementedException();
        }
    }

    // From https://jaspervdj.be/lorem-markdownum/
    [LanguageInjection("markdown")]
    private const string DefaultContents =
"""
# Quid nostro

## Velox tot frugum accipe

Lorem markdownum sacros si Iovis aquarum, oreris bene inmurmurat arborei
propulsa labori invidiosa, sic tibi sic: tumulis. Pectoris ait plectrum fregit
aegram. Cum *legit urit* nec solet corpora loquebatur cur, in vivaque quasque
corpus **sagittas Numam** Lucifer mentesque, **falsa**. Vult plagis sospite
veneni prodiga ratione, currus malus! Et sinat mersaeque fletque cycnus
auxiliaris, sum **quid frondentis** sensit.

1. Moneo ora equos per monstro o foedera
2. Confusura veneni lacertis pisce inmedicabile quid tenuaverat
3. Ardere una quam paciscor alimenta liber
4. Captivarum Venus
5. Nunc iphis Orion aethera genitore doleas pro

Insula illa optime in admonita exigit, clausit sua aut paelicis potiunda ipsius
canes falsisque esset donare. In mea ima loquor, superest vocas densa cognoscere
trepidum inque, restagnantis. Auras est accipiunt erant init turpi tenet isto
voce teretesque facta, quae dederat vultus milite primo. Adspicit ignare
saxumque, tenet fuga male tabuerint umbram *pariterque* nuda vinctumque pugna,
exercita.

## Et tumida

Ut abolere turba dignus, pone respondere comis credo moenia et Cragon nondum
pallenti. Urbem Thracum medii praeclusaque vocanti et senemque per? Deo
genuumque pater, in mihi ruborem: aut mutavit terris removi refert atque
indignave veros, in, promittet! Foeda tempora lux Pholus sit Ligurum quis
[cacumen tamen](http://et.io/cepit.html): hanc.

- Fuisti aptas
- Furibunda arbore passus vulnera quinque Nox menti
- Et gerebat praedae ut duxerat memoranda per
- Nuper Vidi non crines non munusque accusasse
- Trahit opibus vellet rudis

## Habendi ignibus

Ille artus, alma deus est vetus, totidem deprensum arbor lacrimaeque? Illis
Canopo subit lucis tradit [ab](http://www.et-flore.com/pars-spirat.php) certis
mortemque seraque in, **o** vestris omine. *Validum* Lapithaeae, ita orbem dum
praesagaque, dictis, iam disci errore coercuit sit modo Hylactor! Rogat
*iacentes et quaeque* fulva, sertis unguibus quoque possis. Suo Rhodopeius
madefient, mitissima despectus diversa stratis.

1. Diomede aquis
2. Regna mea mota sic usae tu maior
3. Nec hic adunci

Sed esse, prima picum omnes nam patrii do resedit; petebat sed. Amores auras,
potentia subsidunt auras nec: dicar cum dimotis. Fuit Thestias: quam sed hunc
querella erat in inposita. Patuit nomen multi possum quosque erratica patefecit
laudemur: umbrae praedae locus caecis siquidem et cuncti.

""";
}
