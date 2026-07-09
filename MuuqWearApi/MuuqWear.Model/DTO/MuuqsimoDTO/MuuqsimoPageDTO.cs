namespace MuuqWear.Model.DTO.MuuqsimoDTO;

public class MuuqsimoPageDTO
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public MuuqsimoPageContentDTO Content { get; set; } = new();
    public List<MuuqsimoTicketTierDTO> TicketTiers { get; set; } = new();
}

public class MuuqsimoPageContentDTO
{
    public string? Slug { get; set; }
    public string? Tagline { get; set; }
    public string? Eyebrow { get; set; }
    public string? Subtitle { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? CountdownUtc { get; set; }
    public MuuqsimoVenueDTO? Venue { get; set; }
    public MuuqsimoDressCodeDTO? DressCode { get; set; }
    public List<MuuqsimoHeroSlideDTO> HeroSlides { get; set; } = new();
    public MuuqsimoExperienceDTO? Experience { get; set; }
    public MuuqsimoBlueVeilWalkDTO? BlueVeilWalk { get; set; }
    public List<MuuqsimoScheduleEntryDTO> Schedule { get; set; } = new();
    public MuuqsimoRunwayShowDTO? RunwayShow { get; set; }
    public List<MuuqsimoAwardDTO> Awards { get; set; } = new();
    public MuuqsimoTicketsSectionDTO? Tickets { get; set; }
    public List<MuuqsimoTeamMemberDTO> Team { get; set; } = new();
    public List<MuuqsimoSponsorDTO> Sponsors { get; set; } = new();
    public MuuqsimoClosingCtaDTO? ClosingCta { get; set; }
}

public class MuuqsimoVenueDTO
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Accessibility { get; set; }
    public string? Transit { get; set; }
    public string? Parking { get; set; }
}

public class MuuqsimoDressCodeDTO
{
    public string? Theme { get; set; }
    public string? Description { get; set; }
}

public class MuuqsimoHeroSlideDTO
{
    public string ImageUrl { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
}

public class MuuqsimoExperienceDTO
{
    public string? Eyebrow { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public List<MuuqsimoExperienceCardDTO> Cards { get; set; } = new();
}

public class MuuqsimoExperienceCardDTO
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class MuuqsimoBlueVeilWalkDTO
{
    public string? Eyebrow { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public List<MuuqsimoLabelValueDTO> Specs { get; set; } = new();
}

public class MuuqsimoLabelValueDTO
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class MuuqsimoScheduleEntryDTO
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MuuqsimoRunwayShowDTO
{
    public string? Eyebrow { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public List<MuuqsimoCollectionDTO> Collections { get; set; } = new();
    public List<MuuqsimoFeaturedModelDTO> FeaturedModels { get; set; } = new();
    public string? ModelsExtra { get; set; }
    public List<MuuqsimoMusicLightingDTO> MusicLighting { get; set; } = new();
    public List<MuuqsimoProductionCreditDTO> Production { get; set; } = new();
}

public class MuuqsimoCollectionDTO
{
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MuuqsimoFeaturedModelDTO
{
    public string Initials { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Credit { get; set; } = string.Empty;
}

public class MuuqsimoMusicLightingDTO
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MuuqsimoProductionCreditDTO
{
    public string Role { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

public class MuuqsimoAwardDTO
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Prize { get; set; } = string.Empty;
}

public class MuuqsimoTicketsSectionDTO
{
    public string? CapacityNote { get; set; }
    public string? Subtitle { get; set; }
    public string? PressContact { get; set; }
    public List<MuuqsimoAffiliateBenefitDTO> AffiliateBenefits { get; set; } = new();
    public List<MuuqsimoTicketTierConfigDTO> TierConfigs { get; set; } = new();
}

public class MuuqsimoAffiliateBenefitDTO
{
    public string Tier { get; set; } = string.Empty;
    public string Benefit { get; set; } = string.Empty;
    public string DotColor { get; set; } = string.Empty;
}

public class MuuqsimoTicketTierConfigDTO
{
    public Guid ProductId { get; set; }
    public int TotalCapacity { get; set; }
    public List<string> Perks { get; set; } = new();
    public bool IsHighlighted { get; set; }
    public string? Badge { get; set; }
}

public class MuuqsimoTicketTierDTO
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public string Availability { get; set; } = string.Empty;
    public List<string> Perks { get; set; } = new();
    public bool IsHighlighted { get; set; }
    public string? Badge { get; set; }
}

public class MuuqsimoTeamMemberDTO
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class MuuqsimoSponsorDTO
{
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class MuuqsimoClosingCtaDTO
{
    public string? Eyebrow { get; set; }
    public string? Title { get; set; }
    public string? ImageUrl { get; set; }
}

public class MuuqsimoEventSummaryDTO
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? StartDate { get; set; }
}
