using erpWeb.Core.Communs;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.Documents;

public sealed class ServiceDocuments : IServiceDocuments
{
    private readonly IAppDbContext _contexte;
    private readonly IStockageFichiers _stockage;
    private readonly IValidator<DepotDocumentDto> _validateurDepot;
    private readonly IMapper _mapper;

    public ServiceDocuments(IAppDbContext contexte, IStockageFichiers stockage, IValidator<DepotDocumentDto> validateurDepot, IMapper mapper)
    {
        _contexte = contexte;
        _stockage = stockage;
        _validateurDepot = validateurDepot;
        _mapper = mapper;
    }

    public async Task<ResultatOperation<int>> DeposerAsync(DepotDocumentDto depot, CancellationToken jetonAnnulation = default)
    {
        var validation = await _validateurDepot.ValidateAsync(depot, jetonAnnulation);
        if (!validation.IsValid)
        {
            return ResultatOperation<int>.Echec(validation.MessagesErreur());
        }

        var cheminStockage = await _stockage.EnregistrerAsync(depot.Contenu, depot.NomFichier, jetonAnnulation);
        var document = new Document
        {
            TypeEntite = depot.TypeEntite,
            IdEntite = depot.IdEntite,
            NomFichier = depot.NomFichier,
            TypeContenu = depot.TypeContenu,
            Taille = depot.Taille,
            CheminStockage = cheminStockage,
        };

        _contexte.Documents.Add(document);
        try
        {
            await _contexte.SaveChangesAsync(jetonAnnulation);
        }
        catch
        {
            // Pas de fichier orphelin si l'enregistrement en base échoue.
            await _stockage.SupprimerAsync(cheminStockage, CancellationToken.None);
            throw;
        }

        return ResultatOperation<int>.Succes(document.Id);
    }

    public async Task<IReadOnlyList<DocumentDto>> ListerAsync(string? typeEntite, string? idEntite, CancellationToken jetonAnnulation = default)
    {
        var requete = _contexte.Documents.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(typeEntite))
        {
            requete = requete.Where(document => document.TypeEntite == typeEntite);
        }

        if (!string.IsNullOrWhiteSpace(idEntite))
        {
            requete = requete.Where(document => document.IdEntite == idEntite);
        }

        var documents = await requete
            .OrderByDescending(document => document.DateCreation)
            .ToListAsync(jetonAnnulation);

        return _mapper.Map<List<DocumentDto>>(documents);
    }

    public async Task<IReadOnlyList<DocumentDto>> ListerRecentsAsync(int nombreMaximum, CancellationToken jetonAnnulation = default)
    {
        var documents = await _contexte.Documents
            .AsNoTracking()
            .OrderByDescending(document => document.DateCreation)
            .Take(nombreMaximum)
            .ToListAsync(jetonAnnulation);

        return _mapper.Map<List<DocumentDto>>(documents);
    }

    public async Task<FichierDocument?> OuvrirAsync(int id, CancellationToken jetonAnnulation = default)
    {
        var document = await _contexte.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(document => document.Id == id, jetonAnnulation);

        if (document is null)
        {
            return null;
        }

        var contenu = await _stockage.OuvrirAsync(document.CheminStockage, jetonAnnulation);
        return new FichierDocument(_mapper.Map<DocumentDto>(document), contenu);
    }

    public async Task<ResultatOperation> SupprimerAsync(int id, CancellationToken jetonAnnulation = default)
    {
        var document = await _contexte.Documents.FirstOrDefaultAsync(document => document.Id == id, jetonAnnulation);
        if (document is null)
        {
            return ResultatOperation.Echec("Document introuvable.");
        }

        _contexte.Documents.Remove(document);
        await _contexte.SaveChangesAsync(jetonAnnulation);
        await _stockage.SupprimerAsync(document.CheminStockage, jetonAnnulation);
        return ResultatOperation.Succes();
    }
}
