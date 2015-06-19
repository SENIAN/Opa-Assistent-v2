SELECT Familieleden.Telefoonnummer, Persoon.FamilieID, persoon.Achternaam,
Familie.FamilieNaam, Familieleden.PersoonID ,Familieleden.Email, Familieleden.naam FROM Familie, Familieleden, Persoon 
WHERE Persoon.PersoonID = Familieleden.PersoonID AND Persoon.FamilieID = Familie.FamilieID
GROUP BY Familieleden.Telefoonnummer, persoon.FamilieID, persoon.Achternaam,
Familie.FamilieNaam, Familieleden.PersoonID,Familieleden.Email, Familieleden.naam;