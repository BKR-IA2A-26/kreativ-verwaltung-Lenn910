$(document).ready(function () {
    // Initialize DataTables for all standard server-rendered tables
    // Exclude the meta analyzer table as it's populated dynamically
    $('.table-brawl:not(#resultsTable)').DataTable({
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.7/i18n/de-DE.json',
            search: "_INPUT_",
            searchPlaceholder: "Suchen..."
        },
        pageLength: 25,
        lengthMenu: [10, 25, 50, 100],
        dom: '<"row align-items-center mb-3"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
             '<"row"<"col-sm-12"tr>>' +
             '<"row align-items-center mt-3"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        // Disable initial sort to keep our custom backend sorting (like Leaderboard ELO score) intact
        order: [] 
    });
});
