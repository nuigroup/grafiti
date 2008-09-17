using System;
using System.Collections.Generic;
using System.Text;
using Grafiti;
using Grafiti.GestureRecognizers;

namespace GrafitiDemo
{
    class ComposedLinkingGREventArgs : GestureEventArgs
    {
        public ComposedLinkingGREventArgs(string eventId, int groupId)
            : base(eventId, groupId)
        {

        }
    }

    class ComposedLinkingGRConfigurator : GRConfigurator
    {
        public static ComposedLinkingGRConfigurator DEFAULT_CONFIGURATOR = new ComposedLinkingGRConfigurator();

        public MultiTraceGRConfigurator m_multiTraceGRConf;
        public BasicMultiFingerGRConfigurator m_basicMultiFingerGRConf;

        public ComposedLinkingGRConfigurator()
            : base() { }

        public ComposedLinkingGRConfigurator(MultiTraceGRConfigurator multiTraceGRConf, BasicMultiFingerGRConfigurator basicMultiFingerGRConf)
            : base()
        {
            m_multiTraceGRConf = multiTraceGRConf;
            m_basicMultiFingerGRConf = basicMultiFingerGRConf;
        }
    }

    class ComposedLinkingGR : GlobalGestureRecognizer, IGestureListener
    {
        private ComposedLinkingGRConfigurator m_conf;

        public ComposedLinkingGR(GRConfigurator configurator)
            : base(configurator)
        {
            if (!(configurator is ComposedLinkingGRConfigurator))
                Configurator = ComposedLinkingGRConfigurator.DEFAULT_CONFIGURATOR;
            m_conf = (ComposedLinkingGRConfigurator)Configurator;
            
            GestureEventManager.RegisterHandler(typeof(MultiTraceGR), m_conf.m_basicMultiFingerGRConf, "MultiTraceFromTo", OnMultiTraceFromTo);

            DefaultEvents = new string[] { "ComposedLink" };
        }

        public void OnMultiTraceFromTo(object obj, GestureEventArgs args)
        {
            MultiTraceFromToEventArgs cArgs = (MultiTraceFromToEventArgs)args;
            if (cArgs.FromTarget is DemoObject && cArgs.ToTarget is DemoObject)
            {
                DemoObject fromObj = (DemoObject)cArgs.FromTarget;
                DemoObject toObj = (DemoObject)cArgs.ToTarget;
                if (fromObj != toObj)
                {
                    
                }
            }
        } 

        public override void Process(List<Trace> traces)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
