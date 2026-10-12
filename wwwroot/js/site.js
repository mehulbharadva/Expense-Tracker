// App UI interactions for the premium dashboard
document.addEventListener('DOMContentLoaded', function(){
    var sidebar = document.getElementById('appSidebar');
    var toggle = document.getElementById('sidebarToggle');

    function prefersReducedMotion(){
        try{ return window.matchMedia('(prefers-reduced-motion: reduce)').matches; }catch(e){return false}
    }

    if(toggle && sidebar){
        toggle.addEventListener('click', function(e){
            e.preventDefault();
            if(sidebar.classList.contains('open')){
                sidebar.classList.remove('open');
            } else {
                sidebar.classList.add('open');
            }
        });

        // close on outside click for small screens
        document.addEventListener('click', function(e){
            if(window.innerWidth < 992 && sidebar.classList.contains('open')){
                if(!sidebar.contains(e.target) && e.target !== toggle){
                    sidebar.classList.remove('open');
                }
            }
        });
    }

    // subtle hover lift on cards (fallback when reduced motion not requested)
    if(!prefersReducedMotion()){
        document.querySelectorAll('.summary-card, .card, .quick-action').forEach(function(el){
            el.addEventListener('mouseenter', function(){ el.style.transform = 'translateY(-6px)'; });
            el.addEventListener('mouseleave', function(){ el.style.transform = ''; });
        });
    }
});
