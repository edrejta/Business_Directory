# Homepage Backend - Cka eshte shtuar

## Pikat kryesore
- U shtua `HomepageController` i dedikuar per homepage me route baze `""`.
- U shtua endpoint publik per homepage compatibility: `GET /api/homepagecompat/categories`.
- U shtua menaxhimi i promocioneve per role `BusinessOwner/Admin` me `POST /promotions` (me validime per kategori dhe cmime).
- U shtua logjika e rekomandimeve: filtrim me kategori/lokacion dhe personalizim sipas historikut te komenteve te user-it te kycur.
- U shtua kerkimi i avancuar: keyword, kategori, lokacion, `bbox`, radius me koordinata dhe renditje sipas `rating/createdAt`.
- U shtua inferimi i koordinatave per qytete kryesore (fallback) kur biznesi nuk ka lat/lng.
- U shtua logjika e newsletter subscribe me validim email-i dhe `rate limiting` per endpoint-in `subscribe`.
- U shtua llogaritja e rating-ut mesatar nga komentet per listime dhe detaje biznesi.
- U shtua trajtimi i `deals/promotions` me metadata (kategori, cmim origjinal, cmim i zbritur, discount %).

## Ndryshimi i fundit (autorizimi)
- Ne commit-in e fundit te homepage backend, u hoq `[AllowAnonymous]` nga niveli i controller-it dhe u vendos vetem te endpoint-et publike.
- Kjo e ben me te qarte cilat endpoint-e jane publike dhe cilat kerkojne autentikim/autorizim.
